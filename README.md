<h1 align="center">
    NVIDIA Ansel AI Enhancer<br><br>
    <img src="https://i.postimg.cc/xTFLzVpM/Ansel-AI-Res-Up-Show.png" width="400"/>
</h1>

<p align="center">
NVIDIA Ansel AI Enhancer is an app I quickly made that takes advantage of NVIDIA’s Ansel RTX AI Up-Res to allow users to <strong>upscale any image to 8K</strong> whiles adding further quality.
</p>

<br>

<h3 align="center"> 
    <a href="https://github.com/dynamiquel/NVIDIA-Ansel-AI-Enhancer/releases/download/v1.2/NVIDIA.Ansel.AI.Enhancer.exe">🎆 Download the latest version 🎆</a>
</h3>

<br>

# Is this a good upscaler?
Ansel AI is not a great upscaler. The only advantage it has is that it's fast (thanks to using RT cores) and simple to use.

[**ESRGAN**](https://upscale.wiki/wiki/Main_Page) is a far superior way of upscaling images. It is free and works with any GPU (although it does work better with Nvidia GPUs).
The only downside is that it does take more time and is more technical to setup and use. Fortunately, there is a [YouTube tutorial](https://www.youtube.com/watch?v=w4Hb7tyDsWE) that does go through this process for you.

## What are all these models?
A **model** is basically the algorithm the software will use to upscale your images. There are many models to choose from and I can understand that this can be pretty overwhelming. To start off with, I suggest [**4x-UltraSharp**](https://mega.nz/folder/qZRBmaIY#nIG8KyWFcGNTuMX_XNbJ_g). It works great with basically anything. You can then use this as the reference model to test against other models.


# Comparisons
<img src="https://i.postimg.cc/C5yFkC6p/Firefox-NBc.png"/>
<img src="https://i.postimg.cc/yYvMzXX6/Rhaenyr-BC.png"/>

## Prerequisites
- [.NET Framework 4.7.2](https://dotnet.microsoft.com/download/dotnet-framework/thank-you/net472-web-installer) (should already be installed if you have Windows 10)</li>
- [NVIDIA GeForce Experience](https://www.nvidia.com/en-gb/geforce/geforce-experience/)</li>
- NVIDIA display adapter (for greyscale support)</li>
- NVIDIA Turing (20 series) or newer display adapter (for colour support)</li>

## Download
Click [**here**](https://github.com/dynamiquel/NVIDIA-Ansel-AI-Enhancer/releases/download/v1.2/NVIDIA.Ansel.AI.Enhancer.exe) to download the latest version of the app.

## Building (for developers)
### Prerequisites
<ul>
  <li>Visual Studio 2019 for Windows 10</li>
</ul>

Open the **sln** file with Visual Studio 2019. Build > Build Solution.

# How good is it?
Ansel AI is not that great but is a simple and fast way of upscaling your images for free. If you want to take upscaling more seriously, then I suggest you to look into [**ESRGN**](https://upscale.wiki/wiki/Main_Page). The results are **far** superior. It does take more time and is more technical to setup and use, however, there is a [YouTube tutorial](https://www.youtube.com/watch?v=w4Hb7tyDsWE) that does go through this process for you.
ESRGAN does not require an Nvidia GPU but it does work better on any Nvidia GPU (don't need an RTX card).

### Which model do I choose?
There are many models to choose from and I can understand that this can be pretty overwhelming. To start off with, I suggest [**4x-UltraSharp**](https://mega.nz/folder/qZRBmaIY#nIG8KyWFcGNTuMX_XNbJ_g). It works great with basically anything. You can then use this as the reference model to test against other models.
