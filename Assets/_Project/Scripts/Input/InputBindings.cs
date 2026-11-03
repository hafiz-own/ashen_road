namespace AshenRoad.Input
{
    /// <summary>
    /// The action maps as Input System JSON. The project builder writes this to AshenRoadControls.inputactions,
    /// and InputReader falls back to it if no asset is assigned, so there is one source of truth for bindings.
    /// </summary>
    public static class InputBindings
    {
        public const string Gameplay = "Gameplay";
        public const string UI = "UI";

        public const string Json = @"{
    ""name"": ""AshenRoadControls"",
    ""maps"": [
        {
            ""name"": ""Gameplay"",
            ""id"": ""8f6b9f6e-0c0e-4b9a-9a3e-6f1f0e7a1001"",
            ""actions"": [
                { ""name"": ""Move"", ""type"": ""Value"", ""id"": ""8f6b9f6e-0c0e-4b9a-9a3e-6f1f0e7a1101"", ""expectedControlType"": ""Axis"" },
                { ""name"": ""Jump"", ""type"": ""Button"", ""id"": ""8f6b9f6e-0c0e-4b9a-9a3e-6f1f0e7a1102"", ""expectedControlType"": ""Button"" },
                { ""name"": ""Roll"", ""type"": ""Button"", ""id"": ""8f6b9f6e-0c0e-4b9a-9a3e-6f1f0e7a1103"", ""expectedControlType"": ""Button"" },
                { ""name"": ""Attack"", ""type"": ""Button"", ""id"": ""8f6b9f6e-0c0e-4b9a-9a3e-6f1f0e7a1104"", ""expectedControlType"": ""Button"" },
                { ""name"": ""Pause"", ""type"": ""Button"", ""id"": ""8f6b9f6e-0c0e-4b9a-9a3e-6f1f0e7a1105"", ""expectedControlType"": ""Button"" }
            ],
            ""bindings"": [
                { ""name"": ""AD"", ""path"": ""1DAxis"", ""action"": ""Move"", ""isComposite"": true },
                { ""name"": ""negative"", ""path"": ""<Keyboard>/a"", ""action"": ""Move"", ""isPartOfComposite"": true },
                { ""name"": ""positive"", ""path"": ""<Keyboard>/d"", ""action"": ""Move"", ""isPartOfComposite"": true },
                { ""name"": ""Arrows"", ""path"": ""1DAxis"", ""action"": ""Move"", ""isComposite"": true },
                { ""name"": ""negative"", ""path"": ""<Keyboard>/leftArrow"", ""action"": ""Move"", ""isPartOfComposite"": true },
                { ""name"": ""positive"", ""path"": ""<Keyboard>/rightArrow"", ""action"": ""Move"", ""isPartOfComposite"": true },
                { ""name"": ""DPad"", ""path"": ""1DAxis"", ""action"": ""Move"", ""isComposite"": true },
                { ""name"": ""negative"", ""path"": ""<Gamepad>/dpad/left"", ""action"": ""Move"", ""isPartOfComposite"": true },
                { ""name"": ""positive"", ""path"": ""<Gamepad>/dpad/right"", ""action"": ""Move"", ""isPartOfComposite"": true },
                { ""path"": ""<Gamepad>/leftStick/x"", ""action"": ""Move"", ""processors"": ""AxisDeadzone(min=0.25)"" },
                { ""path"": ""<Keyboard>/space"", ""action"": ""Jump"" },
                { ""path"": ""<Keyboard>/w"", ""action"": ""Jump"" },
                { ""path"": ""<Keyboard>/upArrow"", ""action"": ""Jump"" },
                { ""path"": ""<Gamepad>/buttonSouth"", ""action"": ""Jump"" },
                { ""path"": ""<Keyboard>/leftShift"", ""action"": ""Roll"" },
                { ""path"": ""<Keyboard>/rightShift"", ""action"": ""Roll"" },
                { ""path"": ""<Gamepad>/buttonEast"", ""action"": ""Roll"" },
                { ""path"": ""<Gamepad>/rightShoulder"", ""action"": ""Roll"" },
                { ""path"": ""<Keyboard>/j"", ""action"": ""Attack"" },
                { ""path"": ""<Keyboard>/0"", ""action"": ""Attack"" },
                { ""path"": ""<Keyboard>/numpad0"", ""action"": ""Attack"" },
                { ""path"": ""<Mouse>/leftButton"", ""action"": ""Attack"" },
                { ""path"": ""<Gamepad>/buttonWest"", ""action"": ""Attack"" },
                { ""path"": ""<Keyboard>/escape"", ""action"": ""Pause"" },
                { ""path"": ""<Keyboard>/p"", ""action"": ""Pause"" },
                { ""path"": ""<Gamepad>/start"", ""action"": ""Pause"" }
            ]
        },
        {
            ""name"": ""UI"",
            ""id"": ""8f6b9f6e-0c0e-4b9a-9a3e-6f1f0e7a2001"",
            ""actions"": [
                { ""name"": ""Navigate"", ""type"": ""PassThrough"", ""id"": ""8f6b9f6e-0c0e-4b9a-9a3e-6f1f0e7a2101"", ""expectedControlType"": ""Vector2"" },
                { ""name"": ""Submit"", ""type"": ""Button"", ""id"": ""8f6b9f6e-0c0e-4b9a-9a3e-6f1f0e7a2102"", ""expectedControlType"": ""Button"" },
                { ""name"": ""Cancel"", ""type"": ""Button"", ""id"": ""8f6b9f6e-0c0e-4b9a-9a3e-6f1f0e7a2103"", ""expectedControlType"": ""Button"" },
                { ""name"": ""Point"", ""type"": ""PassThrough"", ""id"": ""8f6b9f6e-0c0e-4b9a-9a3e-6f1f0e7a2104"", ""expectedControlType"": ""Vector2"" },
                { ""name"": ""Click"", ""type"": ""PassThrough"", ""id"": ""8f6b9f6e-0c0e-4b9a-9a3e-6f1f0e7a2105"", ""expectedControlType"": ""Button"" }
            ],
            ""bindings"": [
                { ""name"": ""Arrows"", ""path"": ""2DVector"", ""action"": ""Navigate"", ""isComposite"": true },
                { ""name"": ""up"", ""path"": ""<Keyboard>/upArrow"", ""action"": ""Navigate"", ""isPartOfComposite"": true },
                { ""name"": ""down"", ""path"": ""<Keyboard>/downArrow"", ""action"": ""Navigate"", ""isPartOfComposite"": true },
                { ""name"": ""left"", ""path"": ""<Keyboard>/leftArrow"", ""action"": ""Navigate"", ""isPartOfComposite"": true },
                { ""name"": ""right"", ""path"": ""<Keyboard>/rightArrow"", ""action"": ""Navigate"", ""isPartOfComposite"": true },
                { ""name"": ""WASD"", ""path"": ""2DVector"", ""action"": ""Navigate"", ""isComposite"": true },
                { ""name"": ""up"", ""path"": ""<Keyboard>/w"", ""action"": ""Navigate"", ""isPartOfComposite"": true },
                { ""name"": ""down"", ""path"": ""<Keyboard>/s"", ""action"": ""Navigate"", ""isPartOfComposite"": true },
                { ""name"": ""left"", ""path"": ""<Keyboard>/a"", ""action"": ""Navigate"", ""isPartOfComposite"": true },
                { ""name"": ""right"", ""path"": ""<Keyboard>/d"", ""action"": ""Navigate"", ""isPartOfComposite"": true },
                { ""path"": ""<Gamepad>/dpad"", ""action"": ""Navigate"" },
                { ""path"": ""<Gamepad>/leftStick"", ""action"": ""Navigate"" },
                { ""path"": ""<Keyboard>/enter"", ""action"": ""Submit"" },
                { ""path"": ""<Keyboard>/numpadEnter"", ""action"": ""Submit"" },
                { ""path"": ""<Keyboard>/space"", ""action"": ""Submit"" },
                { ""path"": ""<Gamepad>/buttonSouth"", ""action"": ""Submit"" },
                { ""path"": ""<Keyboard>/escape"", ""action"": ""Cancel"" },
                { ""path"": ""<Gamepad>/buttonEast"", ""action"": ""Cancel"" },
                { ""path"": ""<Gamepad>/start"", ""action"": ""Cancel"" },
                { ""path"": ""<Pointer>/position"", ""action"": ""Point"" },
                { ""path"": ""<Mouse>/leftButton"", ""action"": ""Click"" }
            ]
        }
    ],
    ""controlSchemes"": []
}";
    }
}
