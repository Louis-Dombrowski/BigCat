# ACCURATE AS OF 12/24/2024

This file serves to document what the hell all this project's random scripts actually do.
It's in a centralized location, rather than per-script, both to make it obvious when this has become outdated,
and to make learning about the project easier.

# Scripts/Cards/
This directory holds all the logic scripts for the prefabs instantiated by cards from the player's hand. 
(Not the scripts controlling how cards are moved in and out of the hand, see `Card.cs`, `CardHand.cs`)
## BaseCard.cs
This is the abstract interface for card instantiation and placement. it's used by `Card.cs` and implemented by all cards.
It has optional methods for changing the property of a card via the scrollwheel, and handles things like swapping to the edit-mode hologram shader when placing the representations of cards.
## TacticalCucumber.cs
Animates a cucumber launching out of the ground when the cat gets nearby. This sends the cat into a startled state
## YarnBall.cs
Instantiates a shrinking, `Kickable` rigidbody sphere `Distraction`, and makes a trail of yarn follow it as the ball rolls.

# Scripts/Cat/
This directory holds all the scripts for animating and controlling the cat.
## CatController.cs
This does a ton of stuff! It commands the legs to begin their walking animations during steps, animates all the pieces of the cat's body, handles the cat's attention state, pathfinds, and probably more by the time you're reading this.
## CatLeg.cs
This has coroutines to animate legs stepping, and performs inverse kinematics to make the cat's digitigrade joints move properly.
## FieldOfVision.cs
This tracks the `Distraction`s entering and exiting a large, conical trigger representing the cat's FoV. These are used by `CatController.cs` to update the attention state.

# Scripts/StaticBoilerplate/
Everything in this directory is some kind of per-scene singleton.
## GuiData.cs
Updates the score counter and displays notifications when it changes.
## InputHandler.cs
Acts as a middleman between our code and the new InputSystem package. Compartmentalizes all the different input data required by various components.
This is only used when per-frame polling is required; something like `PauseMenu.cs` can get away with just subscribing to an event.
## ModeSwitcher.cs
Switches between edit mode and play mode. This requires additively reloading the scene with `Destructable`s, and copying over the starting template of play mode assets like the cat, and any placed cards.
## PauseMenu.cs
Listens for a pause input, and opens a menu when it receives one.
**NOTE:** This pauses by setting `Time.timeScale` to zero, which can cause problems later! I don't know of a better option, unfortunately.

# Scripts/Util/
These are scripts that could feasibly be copied into another project without issue; things like simple structs with custom property drawers, or helper functions.
## Approximator.cs
This a procedural animation tool that stably approaches a target value with runtime-configurable behavior. It's what makes things like card placement look so smooth.
Implementation is based off of [this](https://www.youtube.com/watch?v=KPoeNZZ6H4s&t=464s) video by [t3ssel8r](https://www.youtube.com/@t3ssel8r)
## ArrowGizmo.cs
This is a collection of functions for drawing vector arrows with gizmos. It's simple, but helps make debug visualizations look cleaner.
## Extensions.cs
This is where we're putting the extension methods of base C#/Unity types.
## FloatRange.cs
This handles clamping a value within a range, lerping between values in a range, and other things of that nature.
Useful for when you want to specify things like a min and max distance, duration, size, etc.
## QuadraticBezier.cs
Wrapper struct for a quadratic bezier curve.
## QuaternionApproximator.cs
The same as the approximator, but a lot less fancy.
It handles stably using `Quaternion.Slerp()` to smoothly approach a target rotation, but it always looks like an exponential decay; you can only change the speed at which it approaches a value.
## Stopwatch.cs
A timer with a nice interface for per-frame updates, and with a pretty progress bar visualization in the editor.
## TubeMesh.cs
Renders a tube with arbitrary length and sides through a set of points. Handles efficient procedural mesh generation and whatnot.

# Scripts/
## CameraController.cs
Controls the camera! Just takes input per-frame and moves the camera accordingly.
## Card.cs
This is specifically the representation for one of the card objects held *in the player's hand*. It handles instantiation of card prefabs inheriting from `BaseCard`, and smoothly approaches a target location in 3D space.
It's tightly coupled to `CardHand.cs`, and expects a lot of its values to be initialized by that script.
## CardHand.cs
This handles all the logic for positioning cards in front of the camera, highlighting cards, picking up cards, placing cards, etc.
## CatDetector.cs
Invokes an editor-configurable list of UnityEvents when the cat comes within a given radius.
## CircularHighlight.cs
Configures a cylinder mesh with a Highlight material
## Destructable.cs
Something the cat can destroy. It holds a monetary value to affect the score, and automatically initializes all child colliders with their own rigidbodies and `DestructablePart`s.
## DestructablePart.cs
**You should never be assigning an object this component manually,** it will be handled for you by `Destructable.cs`.
## Distraction.cs
Whenever its attached trigger enters the cat's FoV, this will be added to the list of things that may entertain the cat.
## GuiNotification.cs
Part of a prefab that automatically scrolls a text box down the screen and makes its alpha fade to zero.
## Kickable.cs
The attached rigidbody will be kicked with a random, large impulse whenever the cat touches it.
## Toggleable.cs
Generic component for toggling between behaviors, see the `TacticalCucumber` prefab for an example.

# Editor/
This is where editor-only scripts go. These can't be included in the final build, and have to be here to be recognized by Unity.
## ApproximatorEditor.cs
The custom property drawer for `Approximator.cs`. Draws a response behavior curve in the editor to help with tuning the approximator values.
## GraphElement.cs
A graphical element to draw a graph of points in the inspector.
## QuaternionApproximatorEditor.cs
The custom property drawer for `QuaternionApproximator.cs`
## ReplaceChildrenWithPrefab.cs
If you made a cool game object, then pasted it around a level, but realize you forgot to make it a prefab and you're now suffering, I have good news. This will replace all children with a specified prefab, while maintaining the old childrens' transforms.
## StopwatchEditor.cs
The custom property drawer for `Stopwatch.cs`. Displays a progress bar.
