# Nara's Unity Tools

Place for some of the tools I wrote for Unity.
Specifically for the creation of (performant) avatars for VRChat and ChilloutVR.

## Installation

Download or clone this project and copy the `Nara` folder into your `Assets` folder in your Unity project.

## Dynamic Bone Constraints

![Screenshot of DBC](media/screenshot.png)

[**>>> Tutorial <<<**](dbc_tutorial.md)

Instead of having 50 dynamic bone transformations hogging up the whole main thread it's better to have just a single chain of dynamic bones and have all other bones be move similarly to the single chain.
This can be done via rotation constraints which are significantly cheaper to compute and they also can be computed on other threads!

The tool is pretty simple to use. Select the objects you want to constrain and click the `From selection` button.
If you already have a chain of bones you want to bind these objects to, then use that as the `Constraint source root`.
`Modify source constraint` will modify (and if needed add) children of the root.
In most cases you can leave the `Constraint source root` empty and just click the magical `Setup constraints` button. [You can have your dynamic bones on your avatar setup in 30 seconds!](https://youtu.be/byvG2FgJEhU)
