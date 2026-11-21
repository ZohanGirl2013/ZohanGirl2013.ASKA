# **ZohanGirl2013.ASKA**

A repository containing the source code for my ASKA mods. (A lot of this is going to contain things that most developers or modders consider obvious or trivial, but I'm using it as notes for myself(and hopefully others) since I'm fairly new to *writing* c# with Harmony patches.)

I'm still learning C# and figuring out the best ways to approach modding, so any suggestions, constructive criticism, or tips are always welcome! If you notice something I could have done better or more efficiently, I'd appreciate the feedback. I'm always interested in understanding *why* one approach might be better than another.

## **Tools I'm Using**
- **Visual Studio** - This one is an obvious given, but I really like the 2022 version for writing, editing, and compiling c#
- **dnSpy** - This is letting my examine bepinex's assemblies so I can look at the games classes, methods, and fields.
- **Cpp2IL** - This is what I used to extract the game into ISIL files, and this is what I have AI explain to me so I understand *how/why* the classes and methods work from what I see in dnSpy.
- **AssetRipper** - This let's me explore game assets, hierarchy prefabs, materials, other other resources and see what MonoBehaviour and scripts they use. the yaml is easier to read than the json in these. (Downloading all the assets does NOT give the same thing as exploring it in the hmtl browser. I've gained nothing from downloading them so far, other than idea's I could do with the icons)
- **UnityExplorer** - This one actually I recommend the most. This actually let's you inspect game objects and pretty much does what all the other tools does except **IN** the actual game with it's own UI layout. You can select hooks(like HarvestInteraction) and then select what methods activate the hook
  - **Compatability Notes(*Getting it to work with ASKA was a bit of a headache*)**
    - Initially, the versions I tried weren't compatible with ASKA's newer Unity version and BepInEx 6 (IL2CPP), so UnityExplorer wouldn't load properly.
    - After searching for a compatible release, I found `UnityExplorer 4.13.6` from yukieiji's GitHub releases. (It's awesome because its a folder with two .dll's that you just put into your plugins folder)
    - Even with that version, the bepinex console initially displayed errors involving SceneHandler.Init(), Int32 to SceneHandle conversion, and Method unstripping failed.
    - **What fixed it:** I changed UnityExplorer's 'Startup_Delay_Time' to ten seconds which allowed the game more time to initialize before it attempted to load the mod.
      - **The Versions Used:**
        - `BepInEx 6.0.0-be.788`
        - `Unity 6000.3.12f1`
        - `UnityExplorer (BepInEx 6 IL2CPP/CoreCLR build)`
    - I'm including this because getting UnityExplorer working with ASKA wasn't straightforward(at least, for me), and hopefully these notes save someone else the same troubleshooting.

### **AI Assistance & Disclaimer**

All of my mods are/will labeled as **AI-Assisted** for transparency.

AI was used extensively to help investigate ASKA's game code, interpret extracted ISIL files, identify relevant classes and methods, and explain programming concepts I was unfamiliar with.

This assistance was particularly useful when investigating cave-wall mechanics, which were difficult to follow even with tools such as AssetRipper and UnityExplorer. The cave walls are something I don't want to look at again:P

AI also assisted with portions of the C# code to help for weird debugging errors or if it could be written/condensed 'cleaner'. For example, one section of Better Mines was unnecessarily complicated, and AI helped simplify it into just four lines from me originally having multiple 'foreach' and if loops(specifically the part where if it equals 1.0 then it's either true/false). This specific contribution is also acknowledged in the Better Mines mod's `.cs` file with '//' notes. 

While AI provided guidance, explanations, and code suggestions, I have also written code independently, made development decisions, and tested the mods in-game to the best of my ability.

I'm sharing my source code both for transparency and in the hope that it may help others learning to mod ASKA.
