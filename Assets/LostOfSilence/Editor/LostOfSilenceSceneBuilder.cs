using System.IO;
using LostOfSilence;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace LostOfSilence.Editor
{
    public static class LostOfSilenceSceneBuilder
    {
        private const string RootFolder = "Assets/LostOfSilence";
        private const string MaterialFolder = RootFolder + "/Materials";
        private const string ScenePath = "Assets/Scenes/LostOfSilence_Game.unity";
        private const string AutoBuildSessionKey = "LostOfSilence.AutoBuildDone";

        private static Material floorMaterial;
        private static Material wallMaterial;
        private static Material woodMaterial;
        private static Material darkWoodMaterial;
        private static Material metalMaterial;
        private static Material fuseMaterial;
        private static Material enemyMaterial;
        private static Material mannequinMaterial;
        private static Material keyMaterial;
        private static Material blueKeyMaterial;
        private static Material redKeyMaterial;
        private static Material greenKeyMaterial;
        private static Material redLightMaterial;

        [InitializeOnLoadMethod]
        private static void QueueAutoBuild()
        {
            EditorApplication.delayCall += AutoBuildIfNeeded;
        }

        private static void AutoBuildIfNeeded()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += AutoBuildIfNeeded;
                return;
            }

            if (SessionState.GetBool(AutoBuildSessionKey, false))
            {
                return;
            }

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null)
            {
                SessionState.SetBool(AutoBuildSessionKey, true);
                return;
            }

            SessionState.SetBool(AutoBuildSessionKey, true);
            Build();
        }

        [MenuItem("Lost Of Silence/Build Playable Prototype")]
        public static void Build()
        {
            EnsureFolders();
            CreateMaterials();

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "LostOfSilence_Game";

            RenderSettings.ambientLight = new Color(0.0015f, 0.0015f, 0.002f);
            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(0.002f, 0.002f, 0.003f);
            RenderSettings.fogDensity = 0.105f;

            GameObject systems = new GameObject("Systems");
            GameManager gameManager = systems.AddComponent<GameManager>();
            AudioListener.pause = false;

            Transform spawn = CreateMarker("PlayerSpawn", new Vector3(0f, 1.05f, -1.1f), Quaternion.Euler(0f, 90f, 0f));
            FirstPersonController player = CreatePlayer(spawn);
            GameObject ui = CreateHud(player.GetComponent<PlayerInteractor>(), gameManager);

            GameObject house = new GameObject("Abandoned House");
            BuildHouse(house.transform, out DoorInteractable exitDoor, out DoorInteractable basementDoor, out Light[] houseLights);
            BuildProps(out Transform mannequin, out Transform[] mannequinPoints);
            BuildItems();
            BuildHidingSpots();
            CreateEnemy(player);
            CreateWatcherEnemy(player);
            BuildNavigation(house);

            PsychologicalEventDirector director = systems.AddComponent<PsychologicalEventDirector>();
            AudioSource eventAudio = systems.AddComponent<AudioSource>();
            eventAudio.spatialBlend = 0f;
            SetSerialized(director, "mannequin", mannequin);
            SetSerialized(director, "mannequinPositions", mannequinPoints);
            SetSerialized(director, "flickerTargets", houseLights);
            SetSerialized(director, "eventAudio", eventAudio);

            SetSerialized(gameManager, "player", player);
            SetSerialized(gameManager, "playerSpawn", spawn);
            SetSerialized(gameManager, "exitDoor", exitDoor);
            SetSerialized(gameManager, "basementDoor", basementDoor);
            BindHud(gameManager, ui);

            gameManager.RegisterPlayer(player);
            gameManager.RegisterSpawn(spawn);
            gameManager.RegisterExitDoor(exitDoor);
            gameManager.RegisterBasementDoor(basementDoor);

            EditorSceneManager.SaveScene(scene, ScenePath);
            AddSceneToBuildSettings(ScenePath);
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
            Debug.Log("Lost Of Silence playable prototype created at " + ScenePath);
        }

        private static void EnsureFolders()
        {
            if (!AssetDatabase.IsValidFolder(RootFolder))
            {
                AssetDatabase.CreateFolder("Assets", "LostOfSilence");
            }

            if (!AssetDatabase.IsValidFolder(MaterialFolder))
            {
                AssetDatabase.CreateFolder(RootFolder, "Materials");
            }
        }

        private static void CreateMaterials()
        {
            floorMaterial = CreateMaterial("Mat_OldFloor", new Color(0.19f, 0.14f, 0.1f), 0.35f);
            wallMaterial = CreateMaterial("Mat_DirtyWall", new Color(0.23f, 0.22f, 0.2f), 0.55f);
            woodMaterial = CreateMaterial("Mat_DampWood", new Color(0.24f, 0.13f, 0.07f), 0.35f);
            darkWoodMaterial = CreateMaterial("Mat_DarkWood", new Color(0.09f, 0.055f, 0.035f), 0.2f);
            metalMaterial = CreateMaterial("Mat_OldMetal", new Color(0.18f, 0.18f, 0.17f), 0.65f);
            fuseMaterial = CreateMaterial("Mat_FuseYellow", new Color(1f, 0.72f, 0.18f), 0.2f);
            enemyMaterial = CreateMaterial("Mat_EnemyBlack", new Color(0.025f, 0.02f, 0.02f), 0.15f);
            mannequinMaterial = CreateMaterial("Mat_Mannequin", new Color(0.72f, 0.68f, 0.6f), 0.45f);
            keyMaterial = CreateMaterial("Mat_Key", new Color(1f, 0.78f, 0.22f), 0.25f);
            blueKeyMaterial = CreateMaterial("Mat_BlueKey", new Color(0.1f, 0.35f, 1f), 0.25f);
            redKeyMaterial = CreateMaterial("Mat_RedKey", new Color(0.85f, 0.06f, 0.04f), 0.25f);
            greenKeyMaterial = CreateMaterial("Mat_GreenKey", new Color(0.1f, 0.75f, 0.22f), 0.25f);
            redLightMaterial = CreateMaterial("Mat_RedWarning", new Color(0.8f, 0.02f, 0.01f), 0.1f);
        }

        private static Material CreateMaterial(string name, Color color, float smoothness)
        {
            string path = MaterialFolder + "/" + name + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null)
                {
                    shader = Shader.Find("Standard");
                }

                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }

            material.color = color;
            material.SetFloat("_Smoothness", smoothness);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static FirstPersonController CreatePlayer(Transform spawn)
        {
            GameObject playerObject = new GameObject("Player");
            playerObject.transform.SetPositionAndRotation(spawn.position, spawn.rotation);

            CharacterController controller = playerObject.AddComponent<CharacterController>();
            controller.height = 1.75f;
            controller.radius = 0.28f;
            controller.center = new Vector3(0f, 0.88f, 0f);

            FirstPersonController firstPerson = playerObject.AddComponent<FirstPersonController>();
            PlayerInteractor interactor = playerObject.AddComponent<PlayerInteractor>();

            GameObject cameraObject = new GameObject("Player Camera");
            cameraObject.transform.SetParent(playerObject.transform);
            cameraObject.transform.localPosition = new Vector3(0f, 1.55f, 0f);
            cameraObject.transform.localRotation = Quaternion.identity;
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.fieldOfView = 72f;
            camera.nearClipPlane = 0.03f;
            cameraObject.AddComponent<AudioListener>();

            GameObject flashlightObject = new GameObject("Flashlight");
            flashlightObject.transform.SetParent(cameraObject.transform);
            flashlightObject.transform.localPosition = new Vector3(0.16f, -0.08f, 0.22f);
            flashlightObject.transform.localRotation = Quaternion.identity;
            Light flashlight = flashlightObject.AddComponent<Light>();
            flashlight.type = LightType.Spot;
            flashlight.range = 10f;
            flashlight.intensity = 5.5f;
            flashlight.spotAngle = 48f;
            flashlight.innerSpotAngle = 26f;
            flashlight.shadows = LightShadows.Soft;

            SetSerialized(firstPerson, "cameraRoot", cameraObject.transform);
            SetSerialized(interactor, "playerCamera", camera);
            SetSerialized(interactor, "flashlight", flashlight);
            return firstPerson;
        }

        private static GameObject CreateHud(PlayerInteractor interactor, GameManager gameManager)
        {
            GameObject canvasObject = new GameObject("HUD");
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            canvasObject.AddComponent<GraphicRaycaster>();

            GameObject gameplayHud = new GameObject("GameplayHud");
            gameplayHud.transform.SetParent(canvasObject.transform, false);
            Stretch(gameplayHud.AddComponent<RectTransform>());

            CreateText(gameplayHud.transform, "ObjectiveText", "Objetivo", new Vector2(22f, -20f), TextAnchor.UpperLeft, 25, new Vector2(760f, 48f));
            CreateText(gameplayHud.transform, "PromptText", "", new Vector2(0f, 90f), TextAnchor.MiddleCenter, 30, new Vector2(760f, 58f));
            CreateText(gameplayHud.transform, "MessageText", "", new Vector2(0f, -115f), TextAnchor.MiddleCenter, 28, new Vector2(920f, 82f));

            Slider stamina = CreateStamina(gameplayHud.transform);
            CreateMobileButton(gameplayHud.transform, "ActionButton", "E", new Vector2(-145f, 125f), interactor.Interact);
            CreateMobileButton(gameplayHud.transform, "FlashlightButton", "F", new Vector2(-145f, 285f), interactor.ToggleFlashlight);

            Button firstMenuButton = CreateMenuScreens(canvasObject.transform, gameManager);

            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            UnityEngine.InputSystem.UI.InputSystemUIInputModule uiModule = eventSystem.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            uiModule.AssignDefaultActions();

            MenuInputController menuInput = canvasObject.AddComponent<MenuInputController>();
            SetSerialized(menuInput, "firstButton", firstMenuButton);

            SetSerialized(gameManager, "hudPanel", gameplayHud);
            SetSerialized(gameManager, "staminaSlider", stamina);
            return canvasObject;
        }

        private static Button CreateMenuScreens(Transform parent, GameManager gameManager)
        {
            GameObject mainMenu = CreateScreenPanel(parent, "MainMenuPanel");
            AddLocalizedText(mainMenu.transform, "Title", "title", new Vector2(0f, 285f), TextAnchor.MiddleCenter, 64, new Vector2(1000f, 90f));
            Button playButton = CreateMenuButton(mainMenu.transform, "PlayButton", "play", new Vector2(0f, 100f), gameManager.StartGame);
            CreateMenuButton(mainMenu.transform, "TutorialButton", "tutorial", new Vector2(0f, -20f), gameManager.ShowTutorial);
            CreateMenuButton(mainMenu.transform, "SettingsButton", "settings", new Vector2(0f, -140f), gameManager.OpenSettingsFromMenu);
            CreateMenuButton(mainMenu.transform, "QuitButton", "quit", new Vector2(0f, -260f), gameManager.QuitGame);

            GameObject tutorial = CreateScreenPanel(parent, "TutorialPanel");
            AddLocalizedText(tutorial.transform, "TutorialTitle", "tutorial", new Vector2(0f, 285f), TextAnchor.MiddleCenter, 50, new Vector2(900f, 80f));
            AddLocalizedText(tutorial.transform, "TutorialBody", "tutorial_body", new Vector2(0f, 40f), TextAnchor.MiddleCenter, 30, new Vector2(1200f, 300f));
            CreateMenuButton(tutorial.transform, "TutorialBackButton", "back", new Vector2(0f, -280f), gameManager.ShowMainMenu);

            GameObject pause = CreateScreenPanel(parent, "PausePanel");
            AddLocalizedText(pause.transform, "PauseTitle", "pause", new Vector2(0f, 255f), TextAnchor.MiddleCenter, 54, new Vector2(900f, 80f));
            CreateMenuButton(pause.transform, "ResumeButton", "resume", new Vector2(0f, 80f), gameManager.ResumeGame);
            CreateMenuButton(pause.transform, "PauseSettingsButton", "settings", new Vector2(0f, -40f), gameManager.OpenSettingsFromPause);
            CreateMenuButton(pause.transform, "RestartButton", "restart", new Vector2(0f, -160f), gameManager.RestartScene);
            CreateMenuButton(pause.transform, "PauseQuitButton", "quit", new Vector2(0f, -280f), gameManager.ShowMainMenu);

            GameObject settings = CreateScreenPanel(parent, "SettingsPanel");
            AddLocalizedText(settings.transform, "SettingsTitle", "settings", new Vector2(0f, 280f), TextAnchor.MiddleCenter, 50, new Vector2(900f, 80f));
            AddLocalizedText(settings.transform, "LanguageLabel", "language", new Vector2(-220f, 120f), TextAnchor.MiddleRight, 30, new Vector2(320f, 50f));
            CreateCompactButton(settings.transform, "PortugueseButton", "PT", new Vector2(0f, 120f), gameManager.SetPortuguese);
            CreateCompactButton(settings.transform, "EnglishButton", "EN", new Vector2(150f, 120f), gameManager.SetEnglish);
            CreateCompactButton(settings.transform, "SpanishButton", "ES", new Vector2(300f, 120f), gameManager.SetSpanish);
            AddLocalizedText(settings.transform, "SensitivityLabel", "sensitivity", new Vector2(-220f, 20f), TextAnchor.MiddleRight, 30, new Vector2(320f, 50f));
            Slider sensitivity = CreateSensitivitySlider(settings.transform, new Vector2(150f, 20f));
            CreateMenuButton(settings.transform, "SettingsBackButton", "back", new Vector2(0f, -245f), gameManager.CloseSettings);

            GameObject victory = CreateScreenPanel(parent, "VictoryPanel");
            AddLocalizedText(victory.transform, "VictoryTitle", "victory", new Vector2(0f, 210f), TextAnchor.MiddleCenter, 58, new Vector2(1000f, 80f));
            AddLocalizedText(victory.transform, "VictoryBody", "victory_body", new Vector2(0f, 80f), TextAnchor.MiddleCenter, 30, new Vector2(1100f, 90f));
            CreateMenuButton(victory.transform, "VictoryRestartButton", "restart", new Vector2(0f, -80f), gameManager.RestartScene);
            CreateMenuButton(victory.transform, "VictoryMenuButton", "back", new Vector2(0f, -200f), gameManager.ShowMainMenu);

            SetSerialized(gameManager, "mainMenuPanel", mainMenu);
            SetSerialized(gameManager, "tutorialPanel", tutorial);
            SetSerialized(gameManager, "pausePanel", pause);
            SetSerialized(gameManager, "settingsPanel", settings);
            SetSerialized(gameManager, "victoryPanel", victory);
            SetSerialized(gameManager, "sensitivitySlider", sensitivity);
            return playButton;
        }

        private static GameObject CreateScreenPanel(Transform parent, string name)
        {
            GameObject panel = new GameObject(name);
            panel.transform.SetParent(parent, false);
            RectTransform rect = panel.AddComponent<RectTransform>();
            Stretch(rect);
            Image image = panel.AddComponent<Image>();
            image.color = new Color(0.015f, 0.013f, 0.011f, 0.88f);
            panel.SetActive(false);
            return panel;
        }

        private static Text AddLocalizedText(Transform parent, string name, string key, Vector2 anchoredPosition, TextAnchor anchor, int fontSize, Vector2 size)
        {
            Text text = CreateText(parent, name, key, anchoredPosition, anchor, fontSize, size);
            LocalizedText localizedText = text.gameObject.AddComponent<LocalizedText>();
            SetSerialized(localizedText, "key", key);
            return text;
        }

        private static Button CreateMenuButton(Transform parent, string name, string labelKey, Vector2 anchoredPosition, UnityEngine.Events.UnityAction callback)
        {
            GameObject buttonObject = new GameObject(name);
            buttonObject.transform.SetParent(parent, false);
            RectTransform rect = buttonObject.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = new Vector2(420f, 76f);

            Image image = buttonObject.AddComponent<Image>();
            image.color = new Color(0.12f, 0.095f, 0.07f, 0.92f);
            Button button = buttonObject.AddComponent<Button>();
            UnityEventTools.AddPersistentListener(button.onClick, callback);

            Text label = AddLocalizedText(buttonObject.transform, "Label", labelKey, Vector2.zero, TextAnchor.MiddleCenter, 28, new Vector2(420f, 76f));
            label.color = new Color(0.96f, 0.86f, 0.58f);
            Stretch(label.rectTransform);
            return button;
        }

        private static Button CreateCompactButton(Transform parent, string name, string label, Vector2 anchoredPosition, UnityEngine.Events.UnityAction callback)
        {
            GameObject buttonObject = new GameObject(name);
            buttonObject.transform.SetParent(parent, false);
            RectTransform rect = buttonObject.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = new Vector2(118f, 62f);

            Image image = buttonObject.AddComponent<Image>();
            image.color = new Color(0.12f, 0.095f, 0.07f, 0.92f);
            Button button = buttonObject.AddComponent<Button>();
            UnityEventTools.AddPersistentListener(button.onClick, callback);

            Text text = CreateText(buttonObject.transform, "Label", label, Vector2.zero, TextAnchor.MiddleCenter, 26, new Vector2(118f, 62f));
            text.color = new Color(0.96f, 0.86f, 0.58f);
            Stretch(text.rectTransform);
            return button;
        }

        private static Dropdown CreateLanguageDropdown(Transform parent, Vector2 anchoredPosition, UnityEngine.Events.UnityAction<int> callback)
        {
            GameObject dropdownObject = new GameObject("LanguageDropdown");
            dropdownObject.transform.SetParent(parent, false);
            RectTransform rect = dropdownObject.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = new Vector2(380f, 62f);

            Image image = dropdownObject.AddComponent<Image>();
            image.color = new Color(0.11f, 0.095f, 0.075f, 0.96f);
            Dropdown dropdown = dropdownObject.AddComponent<Dropdown>();
            dropdown.options.Clear();
            dropdown.options.Add(new Dropdown.OptionData("Portugues"));
            dropdown.options.Add(new Dropdown.OptionData("English"));
            dropdown.options.Add(new Dropdown.OptionData("Espanol"));
            dropdown.value = 0;
            UnityEventTools.AddIntPersistentListener(dropdown.onValueChanged, callback, 0);

            Text label = CreateText(dropdownObject.transform, "Label", "Portugues", Vector2.zero, TextAnchor.MiddleLeft, 26, new Vector2(340f, 62f));
            label.rectTransform.offsetMin = new Vector2(18f, 0f);
            label.rectTransform.offsetMax = new Vector2(-18f, 0f);
            dropdown.captionText = label;

            return dropdown;
        }

        private static Slider CreateSensitivitySlider(Transform parent, Vector2 anchoredPosition)
        {
            GameObject root = new GameObject("SensitivitySlider");
            root.transform.SetParent(parent, false);
            RectTransform rect = root.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = new Vector2(380f, 34f);

            Slider slider = root.AddComponent<Slider>();
            slider.minValue = 0.35f;
            slider.maxValue = 2.5f;
            slider.value = 1f;
            root.AddComponent<SensitivitySliderLink>();

            Image background = AddImage(root.transform, "Background", new Color(0.07f, 0.065f, 0.055f, 0.95f));
            Stretch(background.rectTransform);

            GameObject fillArea = new GameObject("Fill Area");
            fillArea.transform.SetParent(root.transform, false);
            RectTransform fillAreaRect = fillArea.AddComponent<RectTransform>();
            Stretch(fillAreaRect);

            Image fill = AddImage(fillArea.transform, "Fill", new Color(0.91f, 0.72f, 0.32f, 0.95f));
            Stretch(fill.rectTransform);
            slider.fillRect = fill.rectTransform;
            slider.targetGraphic = fill;
            return slider;
        }

        private static Text CreateText(Transform parent, string name, string value, Vector2 anchoredPosition, TextAnchor anchor, int fontSize, Vector2 size)
        {
            GameObject textObject = new GameObject(name);
            textObject.transform.SetParent(parent, false);
            Text text = textObject.AddComponent<Text>();
            text.text = value;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.alignment = anchor;
            text.color = new Color(0.92f, 0.88f, 0.78f);

            RectTransform rect = text.GetComponent<RectTransform>();
            rect.sizeDelta = size;
            rect.anchoredPosition = anchoredPosition;

            if (name == "ObjectiveText")
            {
                rect.anchorMin = new Vector2(0f, 1f);
                rect.anchorMax = new Vector2(0f, 1f);
                rect.pivot = new Vector2(0f, 1f);
            }
            else
            {
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
            }

            return text;
        }

        private static Slider CreateStamina(Transform parent)
        {
            GameObject root = new GameObject("Stamina");
            root.transform.SetParent(parent, false);
            RectTransform rect = root.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(22f, -72f);
            rect.sizeDelta = new Vector2(260f, 16f);

            Slider slider = root.AddComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 1f;
            slider.interactable = false;

            Image background = AddImage(root.transform, "Background", new Color(0.06f, 0.06f, 0.055f, 0.85f));
            Stretch(background.rectTransform);

            GameObject fillArea = new GameObject("Fill Area");
            fillArea.transform.SetParent(root.transform, false);
            RectTransform fillAreaRect = fillArea.AddComponent<RectTransform>();
            Stretch(fillAreaRect);

            Image fill = AddImage(fillArea.transform, "Fill", new Color(0.88f, 0.74f, 0.32f, 0.95f));
            Stretch(fill.rectTransform);
            slider.fillRect = fill.rectTransform;
            slider.targetGraphic = fill;
            return slider;
        }

        private static Image AddImage(Transform parent, string name, Color color)
        {
            GameObject imageObject = new GameObject(name);
            imageObject.transform.SetParent(parent, false);
            Image image = imageObject.AddComponent<Image>();
            image.color = color;
            return image;
        }

        private static void CreateMobileButton(Transform parent, string name, string label, Vector2 anchoredPosition, UnityEngine.Events.UnityAction callback)
        {
            GameObject buttonObject = new GameObject(name);
            buttonObject.transform.SetParent(parent, false);
            RectTransform rect = buttonObject.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = new Vector2(132f, 132f);

            Image image = buttonObject.AddComponent<Image>();
            image.color = new Color(0.08f, 0.075f, 0.065f, 0.62f);
            Button button = buttonObject.AddComponent<Button>();
            UnityEventTools.AddPersistentListener(button.onClick, callback);

            Text text = CreateText(buttonObject.transform, "Label", label, Vector2.zero, TextAnchor.MiddleCenter, 42, new Vector2(132f, 132f));
            text.color = new Color(0.95f, 0.86f, 0.58f);
            Stretch(text.rectTransform);
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void BindHud(GameManager gameManager, GameObject ui)
        {
            SetSerialized(gameManager, "objectiveText", FindDeep(ui.transform, "ObjectiveText").GetComponent<Text>());
            SetSerialized(gameManager, "promptText", FindDeep(ui.transform, "PromptText").GetComponent<Text>());
            SetSerialized(gameManager, "messageText", FindDeep(ui.transform, "MessageText").GetComponent<Text>());
        }

        private static void BuildHouse(Transform parent, out DoorInteractable exitDoor, out DoorInteractable basementDoor, out Light[] houseLights)
        {
            exitDoor = null;
            basementDoor = null;
            GameObject safetyFloor = AddCube("Continuous Safety Floor", new Vector3(6.8f, -0.12f, 0.95f), new Vector3(24f, 0.18f, 17f), floorMaterial, parent);
            safetyFloor.isStatic = true;

            CreateRoom("Initial Bedroom", parent, new Vector3(0f, 0f, 0f), new Vector2(7f, 5f));
            CreateRoom("Main Hall", parent, new Vector3(6.5f, 0f, 0f), new Vector2(6f, 3f));
            CreateRoom("Living Room", parent, new Vector3(13.2f, 0f, 0f), new Vector2(7.4f, 6f));
            CreateRoom("Kitchen", parent, new Vector3(13.2f, 0f, 6.2f), new Vector2(7f, 4.4f));
            CreateRoom("Bathroom", parent, new Vector3(6.5f, 0f, -3.9f), new Vector2(4.2f, 4.8f));
            CreateRoom("Back Bedroom", parent, new Vector3(6.5f, 0f, 3.925f), new Vector2(4.4f, 4.85f));
            CreateSecondFloor(parent);
            CreateBasement(parent);
            CreateExterior(parent);

            float h = 2.7f;
            float t = 0.18f;
            AddWall("Bedroom West Wall", new Vector3(-3.5f, h / 2f, 0f), new Vector3(t, h, 5f), parent);
            AddWall("Bedroom North Wall", new Vector3(0f, h / 2f, 2.5f), new Vector3(7f, h, t), parent);
            AddWall("Bedroom South Wall", new Vector3(0f, h / 2f, -2.5f), new Vector3(7f, h, t), parent);
            AddWall("Bedroom East Top", new Vector3(3.5f, h / 2f, 1.53f), new Vector3(t, h, 1.94f), parent);
            AddWall("Bedroom East Bottom", new Vector3(3.5f, h / 2f, -1.53f), new Vector3(t, h, 1.94f), parent);

            AddWall("Hall South Left", new Vector3(4.7f, h / 2f, -1.5f), new Vector3(2.4f, h, t), parent);
            AddWall("Hall South Right", new Vector3(8.3f, h / 2f, -1.5f), new Vector3(2.4f, h, t), parent);
            AddWall("Hall North Left", new Vector3(4.7f, h / 2f, 1.5f), new Vector3(2.4f, h, t), parent);
            AddWall("Hall North Right", new Vector3(8.3f, h / 2f, 1.5f), new Vector3(2.4f, h, t), parent);

            AddWall("Living West Top", new Vector3(9.5f, h / 2f, 2.25f), new Vector3(t, h, 1.5f), parent);
            AddWall("Living West Bottom", new Vector3(9.5f, h / 2f, -2.25f), new Vector3(t, h, 1.5f), parent);
            AddWall("Living South", new Vector3(13.2f, h / 2f, -3f), new Vector3(7.4f, h, t), parent);
            AddWall("Living North Left", new Vector3(10.9f, h / 2f, 3f), new Vector3(2.8f, h, t), parent);
            AddWall("Living North Right", new Vector3(15.2f, h / 2f, 3f), new Vector3(3.4f, h, t), parent);
            AddWall("Living East Top", new Vector3(16.9f, h / 2f, 1.775f), new Vector3(t, h, 2.45f), parent);
            AddWall("Living East Bottom", new Vector3(16.9f, h / 2f, -1.775f), new Vector3(t, h, 2.45f), parent);

            AddWall("Kitchen West", new Vector3(9.7f, h / 2f, 6.2f), new Vector3(t, h, 4.4f), parent);
            AddWall("Kitchen East", new Vector3(16.7f, h / 2f, 6.2f), new Vector3(t, h, 4.4f), parent);
            AddWall("Kitchen North", new Vector3(13.2f, h / 2f, 8.4f), new Vector3(7f, h, t), parent);
            AddWall("Kitchen South Left", new Vector3(10.9f, h / 2f, 4.0f), new Vector3(2.8f, h, t), parent);
            AddWall("Kitchen South Right", new Vector3(15.2f, h / 2f, 4.0f), new Vector3(3.0f, h, t), parent);
            AddWall("East Connector Wall", new Vector3(16.8f, h / 2f, 3.5f), new Vector3(t, h, 1.0f), parent);

            AddWall("Bathroom West", new Vector3(4.4f, h / 2f, -3.9f), new Vector3(t, h, 4.8f), parent);
            AddWall("Bathroom East", new Vector3(8.6f, h / 2f, -3.9f), new Vector3(t, h, 4.8f), parent);
            AddWall("Bathroom South", new Vector3(6.5f, h / 2f, -6.3f), new Vector3(4.2f, h, t), parent);
            AddWall("Bathroom North Left", new Vector3(5.2f, h / 2f, -1.5f), new Vector3(1.6f, h, t), parent);
            AddWall("Bathroom North Right", new Vector3(7.9f, h / 2f, -1.5f), new Vector3(1.4f, h, t), parent);

            AddWall("Back Bedroom West", new Vector3(4.3f, h / 2f, 3.925f), new Vector3(t, h, 4.85f), parent);
            AddWall("Back Bedroom East", new Vector3(8.7f, h / 2f, 3.925f), new Vector3(t, h, 4.85f), parent);
            AddWall("Back Bedroom North", new Vector3(6.5f, h / 2f, 6.35f), new Vector3(4.4f, h, t), parent);
            AddWall("Back Bedroom South Left", new Vector3(5.1f, h / 2f, 1.5f), new Vector3(1.6f, h, t), parent);
            AddWall("Back Bedroom South Right", new Vector3(7.9f, h / 2f, 1.5f), new Vector3(1.6f, h, t), parent);

            CreateDoor("Bedroom Locked Door", new Vector3(3.58f, 0f, -0.55f), Quaternion.identity, true, false, parent);
            CreateDoor("Bathroom Door", new Vector3(6.0f, 0f, -1.58f), Quaternion.Euler(0f, 90f, 0f), false, false, parent);
            CreateDoor("Back Bedroom Door", new Vector3(5.9f, 0f, 1.58f), Quaternion.Euler(0f, 90f, 0f), false, false, parent);
            exitDoor = CreateDoor("Exit Door", new Vector3(16.98f, 0f, -0.55f), Quaternion.identity, false, true, parent);
            SetSerialized(exitDoor, "requiresAllColoredKeys", true);
            basementDoor = CreateDoor("Basement Door", new Vector3(13.2f, 0f, 3.08f), Quaternion.Euler(0f, 90f, 0f), false, false, parent, ColoredKey.None, true);

            Transform upperArrival = CreateMarker("Upper Floor Arrival", new Vector3(1.5f, 3.08f, 10.4f), Quaternion.Euler(0f, 0f, 0f));
            Transform upperReturn = CreateMarker("Upper Floor Return", new Vector3(2.3f, 1.05f, 1.55f), Quaternion.Euler(0f, 180f, 0f));
            Transform basementArrival = CreateMarker("Basement Arrival", new Vector3(1.5f, -2.92f, -11.2f), Quaternion.Euler(0f, 0f, 0f));
            Transform basementReturn = CreateMarker("Basement Return", new Vector3(13.2f, 1.05f, 2.65f), Quaternion.Euler(0f, 180f, 0f));
            CreateStairTransition("Upper Floor Stairs", new Vector3(2.3f, 0.65f, 2.05f), new Vector3(2.2f, 1.3f, 0.9f), parent, upperArrival, "prompt_use_upper_stairs");
            CreateStairTransition("Upper Floor Stairs Down", new Vector3(1.5f, 3.65f, 10.0f), new Vector3(1.7f, 1.2f, 0.8f), parent, upperReturn, "prompt_use_stairs");
            CreateStairTransition("Basement Stairs", new Vector3(13.2f, 0.65f, 3.9f), new Vector3(1.8f, 1.3f, 0.9f), parent, basementArrival, "prompt_use_basement_stairs");
            CreateStairTransition("Basement Stairs Up", new Vector3(1.5f, -2.35f, -10.8f), new Vector3(1.7f, 1.2f, 0.8f), parent, basementReturn, "prompt_use_stairs");

            Light bedroomLight = CreateCeilingLight("Bedroom Weak Light", new Vector3(0f, 2.35f, 0f), 0.03f, parent);
            Light hallLight = CreateCeilingLight("Hall Flicker Light", new Vector3(6.5f, 2.35f, 0f), 0.045f, parent);
            Light livingLight = CreateCeilingLight("Living Flicker Light", new Vector3(13.2f, 2.35f, 0f), 0.045f, parent);
            Light kitchenLight = CreateCeilingLight("Kitchen Weak Light", new Vector3(13.2f, 2.35f, 6.2f), 0.03f, parent);
            Light bathroomLight = CreateCeilingLight("Bathroom Weak Light", new Vector3(6.5f, 2.35f, -4.7f), 0.025f, parent);
            Light upperLight = CreateCeilingLight("Upper Hall Flicker Light", new Vector3(2.2f, 5.3f, 12.3f), 0.035f, parent);
            Light basementRedLight = CreateCeilingLight("Basement Red Flicker Light", new Vector3(2.2f, -0.65f, -12.4f), 0.45f, parent);
            basementRedLight.color = new Color(1f, 0.02f, 0.01f);

            hallLight.gameObject.AddComponent<FlickerLight>();
            livingLight.gameObject.AddComponent<FlickerLight>();
            upperLight.gameObject.AddComponent<FlickerLight>();
            basementRedLight.gameObject.AddComponent<FlickerLight>();
            foreach (Light sceneLight in new[] { bedroomLight, hallLight, livingLight, kitchenLight, bathroomLight, upperLight, basementRedLight })
            {
                sceneLight.enabled = false;
            }

            houseLights = new[] { bedroomLight, hallLight, livingLight, kitchenLight, bathroomLight, upperLight, basementRedLight };
        }

        private static void CreateSecondFloor(Transform parent)
        {
            float y = 3.0f;
            CreateUpperRoom("Upper Hall", parent, new Vector3(2.0f, y, 12.2f), new Vector2(7.2f, 5.2f));
            CreateUpperRoom("Upper Bedroom A", parent, new Vector3(-2.1f, y, 12.2f), new Vector2(3.8f, 5.2f));
            CreateUpperRoom("Upper Bedroom B", parent, new Vector3(6.2f, y, 12.2f), new Vector2(3.8f, 5.2f));

            float h = 2.5f;
            float cy = y + h / 2f;
            float t = 0.18f;
            AddWall("Upper North Wall", new Vector3(2.0f, cy, 14.8f), new Vector3(10.8f, h, t), parent);
            AddWall("Upper South Wall", new Vector3(2.0f, cy, 9.6f), new Vector3(10.8f, h, t), parent);
            AddWall("Upper West Wall", new Vector3(-3.4f, cy, 12.2f), new Vector3(t, h, 5.2f), parent);
            AddWall("Upper East Wall", new Vector3(7.4f, cy, 12.2f), new Vector3(t, h, 5.2f), parent);
            AddWall("Upper Bedroom A Divider", new Vector3(-0.1f, cy, 12.2f), new Vector3(t, h, 5.2f), parent);
            AddWall("Upper Bedroom B Divider", new Vector3(4.1f, cy, 12.2f), new Vector3(t, h, 5.2f), parent);
            AddCube("Upper Broken Table", new Vector3(2.0f, y + 0.35f, 12.6f), new Vector3(1.4f, 0.2f, 0.9f), darkWoodMaterial, parent);
            AddCube("Upper Bed A", new Vector3(-2.1f, y + 0.35f, 13.5f), new Vector3(1.6f, 0.45f, 1.0f), darkWoodMaterial, parent);
            AddCube("Upper Bed B", new Vector3(6.2f, y + 0.35f, 10.9f), new Vector3(1.6f, 0.45f, 1.0f), darkWoodMaterial, parent);
        }

        private static void CreateBasement(Transform parent)
        {
            float y = -3.0f;
            CreateUpperRoom("Abandoned Basement", parent, new Vector3(2.0f, y, -12.3f), new Vector2(11.5f, 7.6f), true);
            float h = 2.45f;
            float cy = y + h / 2f;
            float t = 0.2f;
            AddWall("Basement North Wall", new Vector3(2.0f, cy, -8.5f), new Vector3(11.5f, h, t), parent);
            AddWall("Basement South Wall", new Vector3(2.0f, cy, -16.1f), new Vector3(11.5f, h, t), parent);
            AddWall("Basement West Wall", new Vector3(-3.75f, cy, -12.3f), new Vector3(t, h, 7.6f), parent);
            AddWall("Basement East Wall", new Vector3(7.75f, cy, -12.3f), new Vector3(t, h, 7.6f), parent);
            AddWall("Basement Storage Divider", new Vector3(2.0f, cy, -12.3f), new Vector3(0.14f, h, 4.2f), parent);
            AddCube("Basement Pipes", new Vector3(-1.8f, y + 1.35f, -15.55f), new Vector3(2.8f, 0.12f, 0.12f), metalMaterial, parent);
            AddCube("Basement Red Warning Panel", new Vector3(7.55f, y + 1.2f, -12.2f), new Vector3(0.08f, 0.9f, 0.65f), redLightMaterial, parent);
        }

        private static void CreateExterior(Transform parent)
        {
            GameObject yardFloor = AddCube("Expanded Walled Yard Floor", new Vector3(25.5f, -0.07f, -0.25f), new Vector3(18.6f, 0.14f, 17.2f), floorMaterial, parent);
            yardFloor.isStatic = true;

            float h = 2.55f;
            float y = h / 2f;
            AddWall("Outer Wall North", new Vector3(25.5f, y, 8.35f), new Vector3(18.6f, h, 0.28f), parent);
            AddWall("Outer Wall South", new Vector3(25.5f, y, -8.85f), new Vector3(18.6f, h, 0.28f), parent);
            AddWall("Outer Wall West Upper", new Vector3(16.2f, y, 5.2f), new Vector3(0.28f, h, 6.3f), parent);
            AddWall("Outer Wall West Lower", new Vector3(16.2f, y, -5.85f), new Vector3(0.28f, h, 6.0f), parent);
            AddWall("Outer Wall East Upper", new Vector3(34.8f, y, 4.25f), new Vector3(0.28f, h, 8.2f), parent);
            AddWall("Outer Wall East Lower", new Vector3(34.8f, y, -5.25f), new Vector3(0.28f, h, 7.2f), parent);

            AddCube("Yard Workbench", new Vector3(22.15f, 0.42f, 4.65f), new Vector3(1.55f, 0.28f, 0.82f), darkWoodMaterial, parent);
            AddCube("Workbench Leg A", new Vector3(21.55f, 0.18f, 4.35f), new Vector3(0.14f, 0.36f, 0.14f), darkWoodMaterial, parent);
            AddCube("Workbench Leg B", new Vector3(22.75f, 0.18f, 4.95f), new Vector3(0.14f, 0.36f, 0.14f), darkWoodMaterial, parent);
            AddCube("Broken Generator", new Vector3(25.45f, 0.48f, -4.65f), new Vector3(1.3f, 0.8f, 0.75f), metalMaterial, parent);
            AddCube("Generator Pipe", new Vector3(25.45f, 1.0f, -4.65f), new Vector3(0.2f, 0.45f, 0.2f), metalMaterial, parent);
            AddCube("Fallen Plank", new Vector3(27.8f, 0.12f, 2.4f), new Vector3(2.2f, 0.12f, 0.24f), woodMaterial, parent).transform.rotation = Quaternion.Euler(0f, 28f, 0f);

            CreateCollectible("Gate Crank", CollectibleKind.GateHandle, new Vector3(22.15f, 0.72f, 4.65f), metalMaterial, new Vector3(0.32f, 0.08f, 0.32f));

            AddCube("Old Yard Shed", new Vector3(29.2f, 0.9f, 5.8f), new Vector3(2.2f, 1.8f, 1.8f), darkWoodMaterial, parent);
            AddCube("Dry Well", new Vector3(31.6f, 0.45f, -6.2f), new Vector3(1.1f, 0.9f, 1.1f), wallMaterial, parent);

            GameObject gate = AddCube("Final Gate", new Vector3(34.82f, 1.05f, -0.55f), new Vector3(0.22f, 2.1f, 2.35f), metalMaterial, parent);
            MarkNotWalkable(gate);
            FinalGateInteractable finalGate = gate.AddComponent<FinalGateInteractable>();
            SetSerialized(finalGate, "gatePanel", gate.transform);

            Light yardLight = CreateCeilingLight("Yard Dying Light", new Vector3(21.1f, 2.45f, -1.25f), 0.12f, parent);
            yardLight.range = 4.2f;
        }

        private static void CreateRoom(string name, Transform parent, Vector3 center, Vector2 size)
        {
            GameObject floor = AddCube(name + " Floor", center + new Vector3(0f, -0.05f, 0f), new Vector3(size.x, 0.1f, size.y), floorMaterial, parent);
            floor.isStatic = true;
            GameObject ceiling = AddCube(name + " Ceiling", center + new Vector3(0f, 2.72f, 0f), new Vector3(size.x, 0.12f, size.y), darkWoodMaterial, parent);
            ceiling.isStatic = true;
            MarkNotWalkable(ceiling);
        }

        private static void CreateUpperRoom(string name, Transform parent, Vector3 center, Vector2 size, bool noMainEnemy = true)
        {
            GameObject floor = AddCube(name + " Floor", center + new Vector3(0f, -0.05f, 0f), new Vector3(size.x, 0.12f, size.y), floorMaterial, parent);
            floor.isStatic = true;
            if (noMainEnemy)
            {
                MarkNotWalkable(floor);
            }

            GameObject ceiling = AddCube(name + " Ceiling", center + new Vector3(0f, 2.55f, 0f), new Vector3(size.x, 0.12f, size.y), darkWoodMaterial, parent);
            ceiling.isStatic = true;
            MarkNotWalkable(ceiling);
        }

        private static GameObject AddWall(string name, Vector3 position, Vector3 scale, Transform parent)
        {
            GameObject wall = AddCube(name, position, scale, wallMaterial, parent);
            wall.isStatic = true;
            MarkNotWalkable(wall);
            return wall;
        }

        private static DoorInteractable CreateDoor(string name, Vector3 hingePosition, Quaternion rotation, bool bedroomKey, bool exit, Transform parent, ColoredKey requiredKey = ColoredKey.None, bool requiresPower = false)
        {
            GameObject root = new GameObject(name);
            root.transform.SetParent(parent);
            root.transform.SetPositionAndRotation(hingePosition, rotation);

            GameObject panel = AddCube("Panel", new Vector3(0f, 1.09f, 0.64f), new Vector3(0.16f, 2.18f, 1.28f), woodMaterial, root.transform, true);
            panel.layer = 0;
            MarkNotWalkable(panel);

            DoorInteractable door = root.AddComponent<DoorInteractable>();
            SetSerialized(door, "hinge", root.transform);
            SetSerialized(door, "requiresBedroomKey", bedroomKey);
            SetSerialized(door, "requiredColoredKey", requiredKey);
            SetSerialized(door, "requiresPower", requiresPower);
            SetSerialized(door, "startsLocked", bedroomKey || exit || requiresPower || requiredKey != ColoredKey.None);
            SetSerialized(door, "isExitDoor", exit);
            SetSerialized(door, "openAngle", bedroomKey ? -95f : 95f);
            return door;
        }

        private static void CreateStairTransition(string name, Vector3 position, Vector3 scale, Transform parent, Transform destination, string promptKey)
        {
            GameObject stairs = AddCube(name, position, scale, darkWoodMaterial, parent);
            stairs.GetComponent<BoxCollider>().isTrigger = true;
            StairTransitionInteractable transition = stairs.AddComponent<StairTransitionInteractable>();
            SetSerialized(transition, "destination", destination);
            SetSerialized(transition, "promptKey", promptKey);
            MarkNotWalkable(stairs);
        }

        private static Light CreateCeilingLight(string name, Vector3 position, float intensity, Transform parent)
        {
            GameObject lightObject = new GameObject(name);
            lightObject.transform.SetParent(parent);
            lightObject.transform.position = position;
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.range = 5.5f;
            light.intensity = intensity;
            light.color = new Color(1f, 0.78f, 0.46f);
            light.shadows = LightShadows.Soft;
            MarkNotWalkable(AddCube(name + " Fixture", position + Vector3.up * 0.08f, new Vector3(0.35f, 0.07f, 0.35f), metalMaterial, parent));
            return light;
        }

        private static void BuildProps(out Transform mannequin, out Transform[] mannequinPoints)
        {
            CreateBed(new Vector3(-1.2f, 0.25f, 1.55f), Quaternion.Euler(0f, 90f, 0f));
            AddCube("Nightstand", new Vector3(1.8f, 0.35f, 1.75f), new Vector3(0.8f, 0.7f, 0.8f), darkWoodMaterial);
            AddCube("Living Sofa", new Vector3(13.1f, 0.45f, -2.25f), new Vector3(2.6f, 0.9f, 0.7f), darkWoodMaterial);
            AddCube("Living Table", new Vector3(13.2f, 0.35f, 0.2f), new Vector3(1.7f, 0.18f, 1.0f), woodMaterial);
            AddCube("Kitchen Counter A", new Vector3(11.0f, 0.45f, 7.55f), new Vector3(1.9f, 0.9f, 0.7f), darkWoodMaterial);
            AddCube("Kitchen Counter B", new Vector3(15.0f, 0.45f, 7.55f), new Vector3(1.9f, 0.9f, 0.7f), darkWoodMaterial);
            AddCube("Bathtub", new Vector3(5.3f, 0.35f, -5.45f), new Vector3(1.55f, 0.7f, 0.82f), metalMaterial);
            AddCube("Bathroom Sink", new Vector3(8.1f, 0.45f, -4.15f), new Vector3(0.55f, 0.9f, 0.5f), metalMaterial);
            AddWindowBoards(new Vector3(-3.6f, 1.35f, 0.9f), Quaternion.Euler(0f, 90f, 12f));
            AddWindowBoards(new Vector3(17.0f, 1.35f, 1.4f), Quaternion.Euler(0f, 90f, -10f));
            AddWindowBoards(new Vector3(13.2f, 1.35f, 8.5f), Quaternion.identity);

            mannequin = CreateMannequin("Moving Mannequin", new Vector3(15.3f, 0f, 1.8f), Quaternion.Euler(0f, 215f, 0f));
            mannequinPoints = new[]
            {
                CreateMarker("Mannequin Point Living", new Vector3(15.3f, 0f, 1.8f), Quaternion.Euler(0f, 215f, 0f)),
                CreateMarker("Mannequin Point Hall", new Vector3(8.4f, 0f, 0.8f), Quaternion.Euler(0f, 275f, 0f)),
                CreateMarker("Mannequin Point Kitchen", new Vector3(10.8f, 0f, 7.1f), Quaternion.Euler(0f, 155f, 0f)),
                CreateMarker("Mannequin Point Bedroom", new Vector3(-2.35f, 0f, -1.7f), Quaternion.Euler(0f, 55f, 0f))
            };
        }

        private static void CreateBed(Vector3 position, Quaternion rotation)
        {
            GameObject bed = new GameObject("Old Bed");
            bed.transform.SetPositionAndRotation(position, rotation);
            AddCube("Frame", new Vector3(0f, 0.22f, 0f), new Vector3(1.2f, 0.3f, 2.05f), darkWoodMaterial, bed.transform, true);
            AddCube("Mattress", new Vector3(0f, 0.48f, 0f), new Vector3(1.1f, 0.22f, 1.9f), wallMaterial, bed.transform, true);
            AddCube("Pillow", new Vector3(0f, 0.66f, 0.68f), new Vector3(0.9f, 0.16f, 0.38f), wallMaterial, bed.transform, true);
        }

        private static void AddWindowBoards(Vector3 position, Quaternion rotation)
        {
            GameObject root = new GameObject("Boarded Window");
            root.transform.SetPositionAndRotation(position, rotation);
            AddCube("Board A", new Vector3(0f, 0.2f, 0f), new Vector3(1.35f, 0.13f, 0.12f), woodMaterial, root.transform, true);
            AddCube("Board B", new Vector3(0f, -0.05f, 0f), new Vector3(1.2f, 0.13f, 0.12f), woodMaterial, root.transform, true).transform.localRotation = Quaternion.Euler(0f, 0f, -16f);
            AddCube("Board C", new Vector3(0f, -0.3f, 0f), new Vector3(1.45f, 0.13f, 0.12f), woodMaterial, root.transform, true);
        }

        private static Transform CreateMannequin(string name, Vector3 position, Quaternion rotation)
        {
            GameObject root = new GameObject(name);
            root.transform.SetPositionAndRotation(position, rotation);
            AddCube("Body", new Vector3(0f, 1.0f, 0f), new Vector3(0.45f, 1.25f, 0.25f), mannequinMaterial, root.transform, true);
            AddCube("Head", new Vector3(0f, 1.78f, 0f), new Vector3(0.32f, 0.32f, 0.32f), mannequinMaterial, root.transform, true);
            AddCube("Left Arm", new Vector3(-0.36f, 1.14f, 0f), new Vector3(0.14f, 0.9f, 0.16f), mannequinMaterial, root.transform, true);
            AddCube("Right Arm", new Vector3(0.36f, 1.14f, 0f), new Vector3(0.14f, 0.9f, 0.16f), mannequinMaterial, root.transform, true);
            AddCube("Base", new Vector3(0f, 0.08f, 0f), new Vector3(0.55f, 0.16f, 0.42f), darkWoodMaterial, root.transform, true);
            return root.transform;
        }

        private static void BuildItems()
        {
            CreateCollectible("Bedroom Key", CollectibleKind.BedroomKey, new Vector3(1.8f, 0.82f, 1.75f), keyMaterial, new Vector3(0.34f, 0.08f, 0.08f));
            CreateCollectible("Fuse Hall", CollectibleKind.Fuse, new Vector3(8.35f, 0.92f, 0.9f), fuseMaterial, new Vector3(0.18f, 0.18f, 0.36f));
            CreateCollectible("Fuse Kitchen", CollectibleKind.Fuse, new Vector3(15.0f, 1.02f, 7.45f), fuseMaterial, new Vector3(0.18f, 0.18f, 0.36f));
            CreateCollectible("Fuse Bathroom", CollectibleKind.Fuse, new Vector3(8.1f, 1.0f, -4.15f), fuseMaterial, new Vector3(0.18f, 0.18f, 0.36f));

            GameObject blueKey = CreateCollectible("Blue Key", CollectibleKind.BlueKey, new Vector3(-2.45f, -2.15f, -14.65f), blueKeyMaterial, new Vector3(0.34f, 0.08f, 0.08f));
            GameObject redKey = CreateCollectible("Red Key", CollectibleKind.RedKey, new Vector3(3.95f, -2.15f, -15.2f), redKeyMaterial, new Vector3(0.34f, 0.08f, 0.08f));
            GameObject greenKey = CreateCollectible("Green Key", CollectibleKind.GreenKey, new Vector3(6.6f, -2.15f, -10.2f), greenKeyMaterial, new Vector3(0.34f, 0.08f, 0.08f));
            blueKey.SetActive(false);
            redKey.SetActive(false);
            greenKey.SetActive(false);

            GameObject note = AddCube("Code Note 413", new Vector3(-2.4f, -2.48f, -10.2f), new Vector3(0.55f, 0.04f, 0.36f), keyMaterial);
            NoteInteractable noteInteractable = note.AddComponent<NoteInteractable>();
            SetSerialized(noteInteractable, "puzzleId", 0);

            GameObject keypad = AddCube("Basement Keypad 413", new Vector3(-0.8f, -2.1f, -9.0f), new Vector3(0.55f, 0.8f, 0.12f), metalMaterial);
            KeypadPuzzleInteractable keypadPuzzle = keypad.AddComponent<KeypadPuzzleInteractable>();
            SetSerialized(keypadPuzzle, "puzzleId", 1);
            SetSerialized(keypadPuzzle, "requiredPuzzleId", 0);
            SetSerialized(keypadPuzzle, "code", "413");
            SetSerialized(keypadPuzzle, "objectToEnable", blueKey);

            GameObject valve = AddCube("Valve Puzzle", new Vector3(3.8f, -2.25f, -14.75f), new Vector3(0.85f, 0.85f, 0.18f), metalMaterial);
            PuzzleStationInteractable valvePuzzle = valve.AddComponent<PuzzleStationInteractable>();
            SetSerialized(valvePuzzle, "puzzleId", 2);
            SetSerialized(valvePuzzle, "promptKey", "prompt_valve");
            SetSerialized(valvePuzzle, "completeKey", "valve_solved");
            SetSerialized(valvePuzzle, "objectToEnable", redKey);

            GameObject breaker = AddCube("Breaker Puzzle", new Vector3(6.85f, -2.15f, -11.0f), new Vector3(0.18f, 0.85f, 0.95f), metalMaterial);
            PuzzleStationInteractable breakerPuzzle = breaker.AddComponent<PuzzleStationInteractable>();
            SetSerialized(breakerPuzzle, "puzzleId", 3);
            SetSerialized(breakerPuzzle, "requiredPuzzleId", 1);
            SetSerialized(breakerPuzzle, "promptKey", "prompt_breaker");
            SetSerialized(breakerPuzzle, "completeKey", "breaker_solved");
            SetSerialized(breakerPuzzle, "objectToEnable", greenKey);

            GameObject fuseBox = AddCube("Fuse Box", new Vector3(16.45f, 1.25f, -2.35f), new Vector3(0.12f, 0.75f, 0.55f), metalMaterial);
            FuseBoxInteractable interactable = fuseBox.AddComponent<FuseBoxInteractable>();
            SetSerialized(interactable, "lightsToEnable", Object.FindObjectsByType<Light>(FindObjectsSortMode.None));
        }

        private static GameObject CreateCollectible(string name, CollectibleKind kind, Vector3 position, Material material, Vector3 scale)
        {
            PrimitiveType primitive = kind == CollectibleKind.Fuse ? PrimitiveType.Cylinder : PrimitiveType.Cube;
            GameObject item = GameObject.CreatePrimitive(primitive);
            item.name = name;
            item.transform.position = position;
            item.transform.localScale = scale;
            item.GetComponent<Renderer>().sharedMaterial = material;
            CollectibleItem collectible = item.AddComponent<CollectibleItem>();
            SetSerialized(collectible, "kind", kind);
            return item;
        }

        private static void BuildHidingSpots()
        {
            CreateHidingSpot("Bedroom Wardrobe", new Vector3(-2.55f, 0.75f, -1.9f), Quaternion.identity, new Vector3(1.0f, 1.5f, 0.5f), new Vector3(-2.55f, 1.05f, -1.55f), new Vector3(-1.65f, 1.05f, -1.25f));
            CreateHidingSpot("Hall Wardrobe", new Vector3(8.65f, 0.75f, 0.95f), Quaternion.Euler(0f, -90f, 0f), new Vector3(1.0f, 1.5f, 0.5f), new Vector3(8.32f, 1.05f, 0.95f), new Vector3(7.85f, 1.05f, 0.25f));
            CreateHidingSpot("Living Table Hide", new Vector3(13.2f, 0.6f, 0.2f), Quaternion.identity, new Vector3(1.7f, 0.75f, 1.1f), new Vector3(13.2f, 0.92f, 0.2f), new Vector3(13.2f, 1.05f, 1.25f));
        }

        private static void CreateHidingSpot(string name, Vector3 position, Quaternion rotation, Vector3 visualScale, Vector3 hidePosition, Vector3 exitPosition)
        {
            GameObject root = AddCube(name, position, visualScale, darkWoodMaterial);
            root.transform.rotation = rotation;
            root.GetComponent<BoxCollider>().isTrigger = true;

            Transform hide = CreateMarker(name + " Hide Point", hidePosition, rotation);
            Transform exit = CreateMarker(name + " Exit Point", exitPosition, rotation);
            HidingSpot hidingSpot = root.AddComponent<HidingSpot>();
            SetSerialized(hidingSpot, "hidePoint", hide);
            SetSerialized(hidingSpot, "exitPoint", exit);
        }

        private static void CreateEnemy(FirstPersonController player)
        {
            Transform spawn = CreateMarker("Enemy Spawn", new Vector3(15.1f, 0f, 2.05f), Quaternion.Euler(0f, 220f, 0f));
            GameObject enemy = new GameObject("Silence Enemy");
            enemy.transform.SetPositionAndRotation(spawn.position, spawn.rotation);
            NavMeshAgent agent = enemy.AddComponent<NavMeshAgent>();
            agent.height = 1.9f;
            agent.radius = 0.32f;
            agent.baseOffset = 0f;
            agent.speed = 1.35f;
            agent.acceleration = 12f;
            agent.angularSpeed = 540f;
            agent.stoppingDistance = 0.35f;
            AddCube("Body", new Vector3(0f, 0.95f, 0f), new Vector3(0.5f, 1.75f, 0.36f), enemyMaterial, enemy.transform, true, false);
            AddCube("Head", new Vector3(0f, 1.92f, 0f), new Vector3(0.38f, 0.38f, 0.38f), enemyMaterial, enemy.transform, true, false);
            AddCube("Left Arm", new Vector3(-0.39f, 1.08f, 0f), new Vector3(0.16f, 1.1f, 0.16f), enemyMaterial, enemy.transform, true, false);
            AddCube("Right Arm", new Vector3(0.39f, 1.08f, 0f), new Vector3(0.16f, 1.1f, 0.16f), enemyMaterial, enemy.transform, true, false);

            Transform[] patrol =
            {
                CreateMarker("Patrol Living", new Vector3(14.7f, 0f, 1.85f), Quaternion.identity),
                CreateMarker("Patrol Hall", new Vector3(7.8f, 0f, 0.55f), Quaternion.identity),
                CreateMarker("Patrol Kitchen", new Vector3(11.2f, 0f, 6.85f), Quaternion.identity),
                CreateMarker("Patrol Bathroom", new Vector3(6.65f, 0f, -4.85f), Quaternion.identity)
            };

            EnemyController controller = enemy.AddComponent<EnemyController>();
            SetSerialized(controller, "player", player);
            SetSerialized(controller, "patrolPoints", patrol);
            SetSerialized(controller, "enemySpawn", spawn);
        }

        private static void CreateWatcherEnemy(FirstPersonController player)
        {
            GameObject watcher = new GameObject("Second Floor Watcher");
            watcher.transform.SetPositionAndRotation(new Vector3(6.1f, 3.05f, 14.1f), Quaternion.Euler(0f, 210f, 0f));
            AddCube("Body", new Vector3(0f, 0.9f, 0f), new Vector3(0.46f, 1.65f, 0.3f), mannequinMaterial, watcher.transform, true, false);
            AddCube("Head", new Vector3(0f, 1.82f, 0f), new Vector3(0.34f, 0.34f, 0.34f), mannequinMaterial, watcher.transform, true, false);
            AddCube("Left Arm", new Vector3(-0.32f, 1.08f, 0f), new Vector3(0.12f, 1.0f, 0.12f), mannequinMaterial, watcher.transform, true, false);
            AddCube("Right Arm", new Vector3(0.32f, 1.08f, 0f), new Vector3(0.12f, 1.0f, 0.12f), mannequinMaterial, watcher.transform, true, false);
            WatcherEnemyController controller = watcher.AddComponent<WatcherEnemyController>();
            SetSerialized(controller, "player", player);
            SetSerialized(controller, "playerCamera", player.GetComponentInChildren<Camera>());
        }

        private static void BuildNavigation(GameObject house)
        {
            Collider[] doorColliders = Object.FindObjectsByType<Collider>(FindObjectsSortMode.None);
            foreach (Collider collider in doorColliders)
            {
                if (collider.GetComponentInParent<DoorInteractable>() != null)
                {
                    collider.enabled = false;
                }
            }

            NavMeshSurface surface = house.AddComponent<NavMeshSurface>();
            surface.collectObjects = CollectObjects.All;
            surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
            surface.agentTypeID = 0;
            surface.BuildNavMesh();

            foreach (Collider collider in doorColliders)
            {
                if (collider != null && collider.GetComponentInParent<DoorInteractable>() != null)
                {
                    collider.enabled = true;
                }
            }
        }

        private static GameObject AddCube(string name, Vector3 position, Vector3 scale, Material material, Transform parent = null, bool local = false, bool keepCollider = true)
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = name;
            if (parent != null)
            {
                cube.transform.SetParent(parent, false);
            }

            cube.transform.localScale = scale;
            if (local)
            {
                cube.transform.localPosition = position;
            }
            else
            {
                cube.transform.position = position;
            }

            Renderer renderer = cube.GetComponent<Renderer>();
            renderer.sharedMaterial = material;

            if (!keepCollider)
            {
                Object.DestroyImmediate(cube.GetComponent<Collider>());
            }

            return cube;
        }

        private static void MarkNotWalkable(GameObject target)
        {
            NavMeshModifier modifier = target.AddComponent<NavMeshModifier>();
            modifier.overrideArea = true;
            modifier.area = 1;
        }

        private static Transform CreateMarker(string name, Vector3 position, Quaternion rotation)
        {
            GameObject marker = new GameObject(name);
            marker.transform.SetPositionAndRotation(position, rotation);
            return marker.transform;
        }

        private static void SetSerialized(Object target, string propertyName, object value)
        {
            SerializedObject serializedObject = new SerializedObject(target);
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property == null)
            {
                Debug.LogWarning("Missing property " + propertyName + " on " + target.name);
                return;
            }

            if (value is Object objectValue)
            {
                property.objectReferenceValue = objectValue;
            }
            else if (value is bool boolValue)
            {
                property.boolValue = boolValue;
            }
            else if (value is float floatValue)
            {
                property.floatValue = floatValue;
            }
            else if (value is int intValue)
            {
                property.intValue = intValue;
            }
            else if (value is string stringValue)
            {
                property.stringValue = stringValue;
            }
            else if (value is System.Array array)
            {
                property.arraySize = array.Length;
                for (int i = 0; i < array.Length; i++)
                {
                    property.GetArrayElementAtIndex(i).objectReferenceValue = array.GetValue(i) as Object;
                }
            }
            else if (value is System.Enum enumValue)
            {
                property.enumValueIndex = System.Convert.ToInt32(enumValue);
            }

            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }

        private static Transform FindDeep(Transform parent, string targetName)
        {
            if (parent.name == targetName)
            {
                return parent;
            }

            foreach (Transform child in parent)
            {
                Transform result = FindDeep(child, targetName);
                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }

        private static void AddSceneToBuildSettings(string scenePath)
        {
            EditorBuildSettingsScene[] currentScenes = EditorBuildSettings.scenes;
            for (int i = 0; i < currentScenes.Length; i++)
            {
                if (currentScenes[i].path == scenePath)
                {
                    currentScenes[i].enabled = true;
                    EditorBuildSettings.scenes = currentScenes;
                    return;
                }
            }

            EditorBuildSettingsScene[] newScenes = new EditorBuildSettingsScene[currentScenes.Length + 1];
            currentScenes.CopyTo(newScenes, 0);
            newScenes[^1] = new EditorBuildSettingsScene(scenePath, true);
            EditorBuildSettings.scenes = newScenes;
        }
    }
}
