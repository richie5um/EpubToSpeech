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

//             cleanzedText = @"
// FOREWORD 
//  BY REED HASTINGS 
// Hard to imagine, but my relationship with Hamilton began purely as a courtesy. Among the many entries on my calendar for Sept. 29, 2004 was a visit from him and Larry Tint, founders of Strategy Capital, a hedge fund investor in Netflix. At that time Netflix was a small DVD-by-mail rental company, and we had only gone public two and a half years before. 
// Typically, in meetings of this type, investors will suss out management, probe for additional color on the company. They are kicking the tires, in other words. But Hamilton and Larry took this sit-down in an entirely—and refreshingly—unexpected direction. Hamilton started with a crisp overview of Power Dynamics, his novel strategy framework, and then utilized that very framework to offer up a penetrating assessment of Netflix’s strategic imperatives. Incisive, extraordinary. The meeting quickly became anything but a courtesy. 
// Hamilton’s impressions stuck with me, and a half-decade later they percolated into an idea. By that point, in 2009, the existential threat from Blockbuster was behind us, and we were on track to reach almost $1.7 billion in sales. These were hard-won advances, but even so our strategy challenges were no less daunting. The clock was ticking on our red envelope business, as DVDs by mail was clearly a transitional technology. And looming was the prospect of facing off against huge competitors with resources far beyond ours: Google, Amazon, Time Warner and Apple to name several. 
// As I had learned over my years as a business person, strategy is an unusual beast. Most of my time and that of everyone else at Netflix must be spent achieving superb execution. Fail at this, and you will surely stumble. Sadly, though, such execution alone will not ensure success. If you don’t get your strategy right, you are at risk. I have been around long enough that I remember the lesson of the IBM PC. Here was a breakthrough product—the customer take-up was amazing: 40,000 upon announcement of the product and more than 100,000 in its first year. No one had ever seen anything like it. IBM’s execution was flawless. Their superb management never missed a beat. It would be hard to imagine another company at that time scaling physical production as rapidly as they did without tripping up. Even their marketing was inspired. Remember Charlie Chaplin as the friendly face of their campaign, welcoming all of us to the new world of computing? 
// But they got the strategy wrong. By outsourcing the OS and permitting Microsoft to sell it to others, IBM squandered their opportunity for the kind of network economy home run that had powered their mainframe juggernaut, System 360. Then their decision to outsource the microprocessor to Intel, while still promoting applications hard-wired to it, likewise ceded yet another important front. As a consequence, they sealed the fate of the PC, rendering it an unattractive box-assembly business. Try as they might, they could never right this ship. The inevitable denouement came with their 2005 fire sale of the business to Lenovo. 
// But rewind to my 2009 problem. The question facing me was this: How could we energetically pursue thoughtful strategizing at Netflix? Fortunately, by this time, we had expended a great deal of effort honing our unique culture—and that provided the key. We could face up to our challenging strategic climate by tapping into the very values we had worked so hard to embed in the company. 
// Our first public “culture deck,” released in August of 2009, identified nine highly valued behaviors. The first was “Judgment.” As we elaborated: You make wise decisions … despite ambiguity You identify root causes and get beyond treating symptoms You think strategically and can articulate what you are and are not trying to do You smartly separate what must be done well now and what can be improved later 
// Wisdom, root causes, thinking strategically, smart prioritization—it made sense to me that all of this mapped to strategy. But to remain true to our culture, senior management could not simply impose its own view of strategy. Instead we had to develop in our people an understanding of the levers of strategy so that, on their own, they could flexibly apply this to their work. Only in this way could we honor another of the pillars of our culture: managing through context, not control. 
// This perspective, however, created a dilemma for me. Strategy is a complex subject—how could this “context” be learned by our people expeditiously? Having held a lifelong interest in education, I have always been much taken with an anecdote concerning the Nobel laureate physicist Richard Feynman, as recounted by James Gleick in his book Genius . Professor Feynman, one of the truly great science teachers of his time, was asked to do a lecture on a difficult area of Quantum Mechanics. Feynman agreed but then several days later recanted, saying “You know I couldn’t do it…That means we really don’t understand it.” 
// In the very same way, our challenge around strategy was clear: did anyone “really understand it” enough to teach it? Fortunately, I recalled the succinctness with which Hamilton summarized strategy in his 2004 presentation. I initiated a dialog with Hamilton and grew more and more convinced of his unique qualifications. In the end, Hamilton developed a program which conveyed to a large number of Netflix’s key people a fundamental understanding of strategy. This effort was a huge success. Still today, many Netflixers look back on it as one of the best educational experiences of their professional lives. 
// Hamilton is so much more than an able synthesizer and communicator, as 7 Powers demonstrates. Any strategy framework, to be broadly useful to a business person, must address all the key strategy issues facing an organization. Hamilton has long been aware of the deficiencies in existing frameworks. His solution? To forge ahead with entirely novel conceptual advances, and then to bind these together into a unified whole. Let me give you two examples of such advances from 7 Powers which stand out for me: Counter-Positioning. Throughout my business career I have often observed powerful incumbents, once lauded for their business acumen, failing to adjust to a new competitive reality. The result is always a stunning fall from grace. A superficial thinker might pin this on lack of vision and leadership. Not Hamilton. By inventing the concept of Counter-Positioning, he was able to peel back the layers to peer into the deeper reality of these situations. Rather than lacking vision, Hamilton established, these incumbents are in fact acting in an entirely predictable and economically rational way. Our earlier battle with Blockbuster bore out this notion. Power Progression. At Netflix, we aggressively prioritize our attention in order to focus on what is essential to accomplish now. This applies to strategy as well: what are the near-in strategic imperatives? Unfortunately, existing strategy frameworks offered little guidance. There was recognition that this was an important issue, but none of those other frameworks could address it in a systematic, reliable, sufficiently transparent way. How did Hamilton respond to this void? Over a span of decades, he developed and refined the Power Progression, illustrating the approximate time fuse for each of the competitive battles facing a business person. It’s an extraordinary advance in the usefulness of strategic thinking. 
// These two advances in understanding are essential for getting to the root of a broad swath of strategy challenges. They are just some of the fruits of my association with Hamilton. Now it’s you who’s in for a treat. 7 Powers tightly integrates the numerous insights he has developed in his several decades of consulting, active equity investing and teaching. It is a uniquely clear and comprehensive distillation of strategy. It will change how you think about business and pull into focus your critical strategy challenges, not to mention their solutions. It may not be the lightest of beach reads; you probably won’t tear through it in a night, but I am confident that your attention will be rewarded many times over. 
// — Reed Hastings CEO and Co-Founder of Netflix
// This book is dedicated to my family—the joy of my life
// ";

            await TextToSpeechAsync(azureConfig, bookFile.Split(Path.DirectorySeparatorChar).Last(), cleanzedText).ConfigureAwait(false);
        }

        static string CleanzeText(string bookText)
        {
            var cleanzedText = bookText;

            cleanzedText = new Regex(@"\r", RegexOptions.Multiline).Replace(cleanzedText, "");
            
            // Clean leading whitespace
            cleanzedText = new Regex(@"^[ \t]+", RegexOptions.Multiline).Replace(cleanzedText, "");
            
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
            // submitUrl = "http://localhost:8111";
            
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
                    
                    // Convert Wav to Mp3
                    await ConvertTTSToMp3Async(outputValue.Name, fileName).ConfigureAwait(false);
                    
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
            ZipFile.ExtractToDirectory(zipName, AppDomain.CurrentDomain.BaseDirectory, true);
            
            var lameEnc = new Mp3Encoder();
            var audioFile = new WavReader();
            
            var pathElements = new List<string> {
                AppDomain.CurrentDomain.BaseDirectory,
                "output.wav"
            };

            audioFile.OpenFile(String.Join(Path.DirectorySeparatorChar, pathElements));

            var srcFormat = audioFile.GetFormat();
            lameEnc.SetFormat(srcFormat, srcFormat);

            var inBuffer = audioFile.readWav();
            var outBuffer = new byte[inBuffer.Length];

            var timer = new System.Diagnostics.Stopwatch();
            timer.Start();
            var len = lameEnc.EncodeBuffer(inBuffer, 0, inBuffer.Length, outBuffer);
            timer.Stop();
            lameEnc.Close();

            var outFile = File.Create($"{fileName}.mp3");
            outFile.Write(outBuffer, 0, len);
            outFile.Close();

            Console.WriteLine($"{DateTime.Now.ToLongTimeString()} Converted to MP3 in {timer.ElapsedMilliseconds / 1000}s");
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