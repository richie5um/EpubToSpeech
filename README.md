> Author: Rich Somerfield

Steps:

1. Create an Azure Cognitive Services - Speech Service (portal.azure.com)
2. Create an `appsettings.json` file (make it copy to output) with:

```
    {
        "Azure": {
            "Region": "<your region, i.e. uksouth>",
            "Key": "<your key>"
        }
    }
```

3. Run `epubtospeech book.epub`
4. Wait (can take a while! - a 500KB epub took about 15 mins total (inc mp3 conversion))
5. If it all works, your outputfile will be `book.epub.mp3`

Note: Code is based on happy-path. Works on my machine. Etc...

