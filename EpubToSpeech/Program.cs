using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using EpubSharp;
using Flurl.Http;
using GroovyCodecs.Mp3;
using GroovyCodecs.WavFile;

namespace EpubToSpeech
{
    class Program
    {
        async static Task Main(string[] args)
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json").Build();

            var section = config.GetSection("Azure");
            var azureConfig = section.Get<AzureConfig>();

            if (args.Length != 1)
            {
                Console.WriteLine("Usage: epubtospeech <book.epub>");
                return;
            }

            var bookFile = args[0];
            
            var book = EpubReader.Read(bookFile);
            var bookText = book.ToPlainText();
            var cleanzedText = CleanzeText(bookText);
            
            await TextToSpeechAsync(azureConfig, bookFile.Split(Path.DirectorySeparatorChar).Last(), cleanzedText).ConfigureAwait(false);
        }

        static string CleanzeText(string bookText)
        {
            var cleanzedText = bookText;

            cleanzedText = new Regex(@"\r", RegexOptions.Multiline).Replace(cleanzedText, "");
            
            // Clean leading whitespace
            cleanzedText = new Regex(@"^[ \t ]+", RegexOptions.Multiline).Replace(cleanzedText, "");
            cleanzedText = new Regex(@"^\s+\.", RegexOptions.Multiline).Replace(cleanzedText, "");
            
            // Clean trailing whitespace
            cleanzedText = new Regex(@"[ \t]+$", RegexOptions.Multiline).Replace(cleanzedText, "");

            // Clean figures
            cleanzedText = new Regex(@"^Figure [0-9\.\-]+.*$", RegexOptions.Multiline | RegexOptions.IgnoreCase).Replace(cleanzedText, "");
            cleanzedText = new Regex(@"^Appendix [0-9\.\-]+.*$", RegexOptions.Multiline | RegexOptions.IgnoreCase).Replace(cleanzedText, "");
            
            // Clean line separators
            cleanzedText = new Regex(@"\n[\n]+").Replace(cleanzedText, "\n\n");
            
            // End every line in a dot
            cleanzedText = new Regex(@"([^\.])$", RegexOptions.Multiline).Replace(cleanzedText, "$1.");
            
            // Remove dot only lines
            cleanzedText = new Regex(@"^(\.)+$", RegexOptions.Multiline).Replace(cleanzedText, "");
            
            return cleanzedText;
        }

        async static Task TextToSpeechAsync(AzureConfig azureConfig, string fileName, string cleanzedText)
        {
            var submitUrl = $"https://{azureConfig.Region}.customvoice.api.speech.microsoft.com/api/texttospeech/v3.0/longaudiosynthesis";
            
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(cleanzedText));
            
            var submitResponse = await submitUrl
                .AllowAnyHttpStatus()
                .WithHeader("Ocp-Apim-Subscription-Key", azureConfig.Key)
                .PostMultipartAsync(p => p
                    .AddString("\"displayname\"", "TTS")
                    .AddString("\"description\"", "TTS")
                    .AddString("\"locale\"", "en-US")
                    .AddString("\"voices\"", "[{\"voicename\": \"en-US-GuyNeural\"}]")
                    .AddString("\"outputformat\"", "riff-16khz-16bit-mono-pcm")
                    .AddString("\"concatenateresult\"", "True")
                    .AddFile("script", stream, "\"text.txt\"", "text/plain")
                )
                .ConfigureAwait(false);
            
            Console.WriteLine($"{DateTime.Now.ToLongTimeString()} Submitted Status: {submitResponse.StatusCode} {String.Join(", ", submitResponse.Headers.Select(h => $"{h.Name}: ${h.Value}"))}");

            if (submitResponse.StatusCode != 202)
            {
                Console.WriteLine($"{DateTime.Now.ToLongTimeString()} Something went wrong");
                
                var responseMessage = submitResponse.ResponseMessage.Content != null ? await submitResponse.ResponseMessage.Content.ReadAsStringAsync().ConfigureAwait(false) : "";
                Console.WriteLine(responseMessage);
                
                return;
            }
            
            var location = submitResponse.Headers.FirstOrDefault("Location");

            while (true)
            {
                var statusResponse = await location
                    .WithHeader("Ocp-Apim-Subscription-Key", azureConfig.Key)
                    .GetJsonAsync<ConvertingResponse>()
                    .ConfigureAwait(false);
                
                Console.WriteLine($"{DateTime.Now.ToLongTimeString()} Converting Status: {statusResponse.Status} {location}");

                if (statusResponse.Status == "Succeeded")
                {
                    break;
                }

                await Task.Delay(60 * 1000).ConfigureAwait(false);
            }

            var outputLocation = location + "/files";

            var outputResponse = await outputLocation
                .WithHeader("Ocp-Apim-Subscription-Key", azureConfig.Key)
                .GetJsonAsync<OutputResponse>()
                .ConfigureAwait(false);

            foreach (var outputValue in outputResponse.Values)
            {
                if (outputValue.Kind == "LongAudioSynthesisResult")
                {
                    Console.WriteLine($"{DateTime.Now.ToLongTimeString()} Downloading: {outputValue.Name}");
                    await outputValue.Links
                        .ContentUrl
                        .DownloadFileAsync(AppDomain.CurrentDomain.BaseDirectory, outputValue.Name).ConfigureAwait(false);
                    Console.WriteLine($"{DateTime.Now.ToLongTimeString()} Downloaded: {outputValue.Name}");
                    
                    Console.WriteLine($"{DateTime.Now.ToLongTimeString()} Converting To Mp3: {outputValue.Name}");
                    await ConvertTTSToMp3Async(outputValue.Name, fileName).ConfigureAwait(false);
                    Console.WriteLine($"{DateTime.Now.ToLongTimeString()} Converted To Mp3: {outputValue.Name}");
                    
                    break;
                }
                else
                {
                    Console.WriteLine($"{DateTime.Now.ToLongTimeString()} Skipping: {outputValue.Name}");
                }
            }
            
            // Remove request
            var removeResponse = await location
                .WithHeader("Ocp-Apim-Subscription-Key",azureConfig.Key)
                .DeleteAsync()
                .ConfigureAwait(false);
            
            Console.WriteLine($"{DateTime.Now.ToLongTimeString()} Removing Remote: {removeResponse.StatusCode}");

            Console.WriteLine($"{DateTime.Now.ToLongTimeString()} Finished");
        }

        async static Task ConvertTTSToMp3Async(string zipName, string fileName)
        {
            var zipPathElements = new List<string> {
                AppDomain.CurrentDomain.BaseDirectory,
                zipName
            };
            
            ZipFile.ExtractToDirectory(String.Join(Path.DirectorySeparatorChar, zipPathElements), AppDomain.CurrentDomain.BaseDirectory, true);
            
            var lameEnc = new Mp3Encoder();
            var audioFile = new WavReader();
            
            var wavPathElements = new List<string> {
                AppDomain.CurrentDomain.BaseDirectory,
                "output.wav"
            };

            audioFile.OpenFile(String.Join(Path.DirectorySeparatorChar, wavPathElements));

            var srcFormat = audioFile.GetFormat();
            lameEnc.SetFormat(srcFormat, srcFormat);

            var inBuffer = audioFile.readWav();
            var outBuffer = new byte[inBuffer.Length];

            var len = lameEnc.EncodeBuffer(inBuffer, 0, inBuffer.Length, outBuffer);
            lameEnc.Close();

            var outFile = File.Create($"{fileName}.mp3");
            outFile.Write(outBuffer, 0, len);
            outFile.Close();
        }
    }

    internal class ConvertingResponse
    {
        public string Status { get; set; }
    }
    
    internal class OutputResponse
    {
        public IEnumerable<OutputResponseValue> Values { get; set; }
    }

    internal class OutputResponseValue
    {
        public string Name { get; set; }
        public string Kind { get; set; }
        public OutputResponseLink Links { get; set; }
    }
    
    internal class OutputResponseLink
    {
        public string ContentUrl { get; set; }
    }

    internal class AzureConfig
    {
        public string Region { get; set; }
        public string Key { get; set; }
    }
}