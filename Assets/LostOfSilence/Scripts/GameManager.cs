using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace LostOfSilence
{
    public enum GameLanguage
    {
        Portuguese,
        English,
        Spanish
    }

    public sealed class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Scene")]
        [SerializeField] private FirstPersonController player;
        [SerializeField] private Transform playerSpawn;
        [SerializeField] private DoorInteractable exitDoor;
        [SerializeField] private DoorInteractable basementDoor;

        [Header("HUD")]
        [SerializeField] private GameObject hudPanel;
        [SerializeField] private Text promptText;
        [SerializeField] private Text objectiveText;
        [SerializeField] private Text messageText;
        [SerializeField] private Slider staminaSlider;

        [Header("Screens")]
        [SerializeField] private GameObject mainMenuPanel;
        [SerializeField] private GameObject tutorialPanel;
        [SerializeField] private GameObject pausePanel;
        [SerializeField] private GameObject settingsPanel;
        [SerializeField] private GameObject victoryPanel;

        [Header("Settings")]
        [SerializeField] private Dropdown languageDropdown;
        [SerializeField] private Slider sensitivitySlider;

        [Header("Progress")]
        [SerializeField] private int fusesNeeded = 3;

        private int fusesCollected;
        private bool hasBedroomKey;
        private bool hasGateHandle;
        private bool hasBlueKey;
        private bool hasRedKey;
        private bool hasGreenKey;
        private bool powerRestored;
        private readonly bool[] solvedPuzzles = new bool[4];
        private bool gameStarted;
        private bool victory;
        private bool settingsOpenedFromPause;
        private Coroutine messageRoutine;
        private GameLanguage language = GameLanguage.Portuguese;

        public bool HasBedroomKey => hasBedroomKey;
        public bool HasAllFuses => fusesCollected >= fusesNeeded;
        public bool HasGateHandle => hasGateHandle;
        public bool IsPowerRestored => powerRestored;
        public bool HasAllColoredKeys => hasBlueKey && hasRedKey && hasGreenKey;
        public bool HasSolvedAllPuzzles => solvedPuzzles[0] && solvedPuzzles[1] && solvedPuzzles[2] && solvedPuzzles[3];
        public bool GameplayInputActive => gameStarted && !victory && Time.timeScale > 0f && !IsAnyMenuOpen();

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            ApplySensitivity(sensitivitySlider != null ? sensitivitySlider.value : 1f);
            SetLanguage(languageDropdown != null ? languageDropdown.value : 0);
            ShowMainMenu();
            UpdateObjective();
        }

        private void Update()
        {
            if (!gameStarted || victory)
            {
                return;
            }

            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
            {
                if (settingsPanel != null && settingsPanel.activeSelf)
                {
                    CloseSettings();
                }
                else if (pausePanel != null && pausePanel.activeSelf)
                {
                    ResumeGame();
                }
                else
                {
                    PauseGame();
                }
            }
        }

        public void RegisterPlayer(FirstPersonController controller)
        {
            player = controller;
        }

        public void RegisterSpawn(Transform spawn)
        {
            playerSpawn = spawn;
        }

        public void RegisterExitDoor(DoorInteractable door)
        {
            exitDoor = door;
        }

        public void RegisterBasementDoor(DoorInteractable door)
        {
            basementDoor = door;
        }

        public void StartGame()
        {
            gameStarted = true;
            victory = false;
            SetPanels(false, false, false, false, false);
            SetHudVisible(true);
            SetPaused(false);
            ShowMessage(Localize("start_message"));
            UpdateObjective();
        }

        public void ShowMainMenu()
        {
            gameStarted = false;
            SetPanels(true, false, false, false, false);
            SetHudVisible(false);
            SetPaused(true);
        }

        public void ShowTutorial()
        {
            SetPanels(false, true, false, false, false);
            SetHudVisible(false);
            SetPaused(true);
        }

        public void PauseGame()
        {
            if (!gameStarted || victory)
            {
                return;
            }

            SetPanels(false, false, true, false, false);
            SetHudVisible(true);
            SetPaused(true);
        }

        public void ResumeGame()
        {
            if (!gameStarted || victory)
            {
                return;
            }

            SetPanels(false, false, false, false, false);
            SetHudVisible(true);
            SetPaused(false);
        }

        public void OpenSettingsFromMenu()
        {
            settingsOpenedFromPause = false;
            SetPanels(false, false, false, true, false);
            SetPaused(true);
        }

        public void OpenSettingsFromPause()
        {
            settingsOpenedFromPause = true;
            SetPanels(false, false, false, true, false);
            SetPaused(true);
        }

        public void CloseSettings()
        {
            if (settingsOpenedFromPause && gameStarted)
            {
                PauseGame();
            }
            else
            {
                ShowMainMenu();
            }
        }

        public void RestartScene()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        public void QuitGame()
        {
            Application.Quit();
        }

        public void SetLanguage(int index)
        {
            language = (GameLanguage)Mathf.Clamp(index, 0, 2);
            if (languageDropdown != null && languageDropdown.value != index)
            {
                languageDropdown.value = index;
            }

            foreach (LocalizedText localizedText in FindObjectsByType<LocalizedText>(FindObjectsSortMode.None))
            {
                localizedText.Refresh();
            }

            UpdateObjective();
        }

        public void SetPortuguese()
        {
            SetLanguage(0);
        }

        public void SetEnglish()
        {
            SetLanguage(1);
        }

        public void SetSpanish()
        {
            SetLanguage(2);
        }

        public void SetSensitivity(float value)
        {
            ApplySensitivity(value);
        }

        public void SetPrompt(string prompt)
        {
            if (promptText != null)
            {
                promptText.text = prompt;
            }
        }

        public void UpdateStamina(float value)
        {
            if (staminaSlider != null)
            {
                staminaSlider.value = Mathf.Clamp01(value);
            }
        }

        public void CollectKey()
        {
            hasBedroomKey = true;
            ShowMessage(Localize("key_collected"));
            UpdateObjective();
        }

        public void CollectFuse()
        {
            fusesCollected++;
            ShowMessage(Localize("fuse_collected") + " " + fusesCollected + "/" + fusesNeeded);
            UpdateObjective();
        }

        public bool TryRestorePower()
        {
            if (!HasAllFuses)
            {
                ShowMessage(Localize("missing_fuses") + " " + fusesCollected + "/" + fusesNeeded);
                return false;
            }

            if (basementDoor != null)
            {
                basementDoor.Unlock();
            }

            powerRestored = true;
            ShowMessage(Localize("power_restored"));
            UpdateObjective();
            return true;
        }

        public void CollectGateHandle()
        {
            hasGateHandle = true;
            ShowMessage(Localize("gate_handle_collected"));
            UpdateObjective();
        }

        public void CollectColoredKey(ColoredKey key)
        {
            switch (key)
            {
                case ColoredKey.Blue:
                    hasBlueKey = true;
                    ShowMessage(Localize("blue_key_collected"));
                    break;
                case ColoredKey.Red:
                    hasRedKey = true;
                    ShowMessage(Localize("red_key_collected"));
                    break;
                case ColoredKey.Green:
                    hasGreenKey = true;
                    ShowMessage(Localize("green_key_collected"));
                    break;
            }

            UpdateObjective();
        }

        public bool HasColoredKey(ColoredKey key)
        {
            return key switch
            {
                ColoredKey.Blue => hasBlueKey,
                ColoredKey.Red => hasRedKey,
                ColoredKey.Green => hasGreenKey,
                _ => true
            };
        }

        public void CompletePuzzle(int puzzleId)
        {
            if (puzzleId < 0 || puzzleId >= solvedPuzzles.Length)
            {
                return;
            }

            solvedPuzzles[puzzleId] = true;
            UpdateObjective();
        }

        public bool IsPuzzleComplete(int puzzleId)
        {
            return puzzleId >= 0 && puzzleId < solvedPuzzles.Length && solvedPuzzles[puzzleId];
        }

        public bool TryOpenFinalGate()
        {
            if (!powerRestored)
            {
                ShowMessage(Localize("gate_no_power"));
                return false;
            }

            if (!hasGateHandle)
            {
                ShowMessage(Localize("gate_missing_handle"));
                return false;
            }

            ShowMessage(Localize("gate_opened"), 2f);
            Escape();
            return true;
        }

        public void RespawnPlayer()
        {
            if (player == null || playerSpawn == null)
            {
                return;
            }

            player.SetHidden(false);
            player.Teleport(playerSpawn.position, playerSpawn.rotation);
            ShowMessage(Localize("caught"));
        }

        public void Escape()
        {
            victory = true;
            SetPanels(false, false, false, false, true);
            SetHudVisible(false);
            SetPaused(true);
        }

        public void CloseOpenMenuWithEscape()
        {
            if (settingsPanel != null && settingsPanel.activeSelf)
            {
                CloseSettings();
            }
            else if (tutorialPanel != null && tutorialPanel.activeSelf)
            {
                ShowMainMenu();
            }
            else if (pausePanel != null && pausePanel.activeSelf)
            {
                ResumeGame();
            }
        }

        public void ShowMessage(string message, float duration = 4f)
        {
            if (messageText == null)
            {
                return;
            }

            if (messageRoutine != null)
            {
                StopCoroutine(messageRoutine);
            }

            messageRoutine = StartCoroutine(MessageRoutine(message, duration));
        }

        public string Localize(string key)
        {
            return language switch
            {
                GameLanguage.English => English(key),
                GameLanguage.Spanish => Spanish(key),
                _ => Portuguese(key)
            };
        }

        private void ApplySensitivity(float value)
        {
            float sensitivity = Mathf.Clamp(value, 0.35f, 2.5f);
            if (sensitivitySlider != null && !Mathf.Approximately(sensitivitySlider.value, sensitivity))
            {
                sensitivitySlider.value = sensitivity;
            }

            if (player != null)
            {
                player.SetLookSensitivity(sensitivity);
            }
        }

        private IEnumerator MessageRoutine(string message, float duration)
        {
            messageText.text = message;
            messageText.enabled = true;
            yield return new WaitForSecondsRealtime(duration);
            messageText.enabled = false;
            messageRoutine = null;
        }

        private void UpdateObjective()
        {
            if (objectiveText == null)
            {
                return;
            }

            if (!hasBedroomKey)
            {
                objectiveText.text = Localize("objective_key");
            }
            else if (!HasAllFuses)
            {
                objectiveText.text = Localize("objective_fuses") + " " + fusesCollected + "/" + fusesNeeded;
            }
            else if (!powerRestored)
            {
                objectiveText.text = Localize("objective_restore_power");
            }
            else if (!HasSolvedAllPuzzles)
            {
                objectiveText.text = Localize("objective_basement_puzzles");
            }
            else if (!HasAllColoredKeys)
            {
                objectiveText.text = Localize("objective_colored_keys");
            }
            else if (!hasGateHandle)
            {
                objectiveText.text = Localize("objective_exit_house");
            }
            else
            {
                objectiveText.text = Localize("objective_final_gate");
            }
        }

        private void SetPanels(bool menu, bool tutorial, bool pause, bool settings, bool win)
        {
            if (mainMenuPanel != null) mainMenuPanel.SetActive(menu);
            if (tutorialPanel != null) tutorialPanel.SetActive(tutorial);
            if (pausePanel != null) pausePanel.SetActive(pause);
            if (settingsPanel != null) settingsPanel.SetActive(settings);
            if (victoryPanel != null) victoryPanel.SetActive(win);
        }

        private void SetHudVisible(bool visible)
        {
            if (hudPanel != null)
            {
                hudPanel.SetActive(visible);
            }
        }

        private void SetPaused(bool paused)
        {
            Time.timeScale = paused ? 0f : 1f;
            if (player != null)
            {
                player.SetFrozen(paused);
            }

            Cursor.visible = paused;
            Cursor.lockState = paused ? CursorLockMode.None : CursorLockMode.Locked;
        }

        private bool IsAnyMenuOpen()
        {
            return (mainMenuPanel != null && mainMenuPanel.activeSelf)
                || (tutorialPanel != null && tutorialPanel.activeSelf)
                || (pausePanel != null && pausePanel.activeSelf)
                || (settingsPanel != null && settingsPanel.activeSelf)
                || (victoryPanel != null && victoryPanel.activeSelf);
        }

        private static string Portuguese(string key)
        {
            return key switch
            {
                "title" => "LOST OF SILENCE",
                "play" => "JOGAR",
                "tutorial" => "TUTORIAL",
                "settings" => "CONFIGURACOES",
                "quit" => "SAIR",
                "back" => "VOLTAR",
                "resume" => "CONTINUAR",
                "restart" => "REINICIAR",
                "pause" => "PAUSA",
                "victory" => "VOCE ESCAPOU",
                "victory_body" => "O portao rangeu e abriu para a estrada. A casa ficou em silencio para tras.",
                "language" => "IDIOMA",
                "sensitivity" => "SENSIBILIDADE",
                "tutorial_body" => "WASD move | Mouse olha | Shift corre | Ctrl agacha | E interage | F lanterna | Esc pausa\nLeia notas, use codigos, resolva mecanismos, pegue chaves coloridas e evite olhar para longe do inimigo do segundo andar.",
                "start_message" => "Voce acordou no dormitorio. Ache a chave para sair.",
                "key_collected" => "Chave do dormitorio coletada.",
                "fuse_collected" => "Fusivel coletado:",
                "missing_fuses" => "Faltam fusivels:",
                "power_restored" => "A energia voltou parcialmente. A porta do subsolo destravou.",
                "gate_handle_collected" => "Manivela do portao coletada.",
                "blue_key_collected" => "Chave azul coletada.",
                "red_key_collected" => "Chave vermelha coletada.",
                "green_key_collected" => "Chave verde coletada.",
                "gate_missing_handle" => "Falta uma manivela para abrir este portao.",
                "gate_no_power" => "O mecanismo esta morto. Restaure a energia primeiro.",
                "gate_opened" => "O portao externo abriu.",
                "caught" => "Ele te encontrou. Voce acorda novamente no dormitorio.",
                "objective_key" => "Objetivo: achar a chave do dormitorio",
                "objective_fuses" => "Objetivo: coletar fusivels",
                "objective_restore_power" => "Objetivo: religar energia na caixa de fusivels",
                "objective_basement_puzzles" => "Objetivo: descer ao subsolo e resolver os 4 desafios",
                "objective_colored_keys" => "Objetivo: pegar as chaves azul, vermelha e verde",
                "objective_exit_house" => "Objetivo: abrir a porta principal e procurar a manivela no patio",
                "objective_escape" => "Objetivo: sair pela porta destravada",
                "objective_outside_handle" => "Objetivo: procure uma manivela no quintal",
                "objective_final_gate" => "Objetivo: abrir o portao externo",
                "prompt_key" => "E - Pegar chave",
                "prompt_fuse" => "E - Pegar fusivel",
                "prompt_gate_handle" => "E - Pegar manivela",
                "prompt_blue_key" => "E - Pegar chave azul",
                "prompt_red_key" => "E - Pegar chave vermelha",
                "prompt_green_key" => "E - Pegar chave verde",
                "prompt_final_gate" => "E - Abrir portao",
                "prompt_read_note" => "E - Ler nota",
                "prompt_read_again" => "E - Ler novamente",
                "prompt_keypad" => "E - Usar teclado",
                "prompt_valve" => "E - Girar valvulas",
                "prompt_breaker" => "E - Ajustar disjuntores",
                "prompt_symbol" => "E - Alinhar simbolos",
                "prompt_puzzle" => "E - Resolver mecanismo",
                "prompt_puzzle_done" => "Resolvido",
                "prompt_use_stairs" => "E - Usar escada",
                "prompt_use_basement_stairs" => "E - Descer ao subsolo",
                "prompt_use_upper_stairs" => "E - Subir ao segundo andar",
                "prompt_place_fuses" => "E - Colocar fusivels",
                "prompt_power_done" => "Energia restaurada",
                "prompt_use_key" => "E - Usar chave",
                "prompt_locked" => "Trancada",
                "prompt_escape" => "E - Escapar",
                "prompt_open" => "E - Abrir porta",
                "prompt_close" => "E - Fechar porta",
                "prompt_hide" => "E - Esconder",
                "prompt_leave" => "E - Sair",
                "door_unlocked" => "A chave gira devagar. A porta abriu.",
                "door_locked" => "A porta esta trancada.",
                "door_need_blue" => "Esta porta precisa da chave azul.",
                "door_need_red" => "Esta porta precisa da chave vermelha.",
                "door_need_green" => "Esta porta precisa da chave verde.",
                "door_need_power" => "Sem energia. Ligue os fusivels primeiro.",
                "door_need_main_keys" => "A porta principal tem tres fechaduras: azul, vermelha e verde.",
                "note_code_message" => "A nota diz: 'O cofre antigo aceita 413. Nao confie nas luzes vermelhas.'",
                "keypad_start" => "Digite o codigo no teclado numerico.",
                "keypad_typing" => "Codigo:",
                "keypad_solved" => "O teclado aceitou o codigo.",
                "keypad_wrong" => "Codigo errado. O teclado apagou.",
                "keypad_needs_note" => "Voce precisa ler a nota primeiro.",
                "puzzle_missing_step" => "Ainda falta uma pista antes disso.",
                "valve_solved" => "As valvulas pararam de chiar. Algo destravou.",
                "breaker_solved" => "Os disjuntores encaixaram na ordem certa.",
                "symbol_solved" => "Os simbolos ficaram alinhados.",
                "puzzle_solved" => "Mecanismo resolvido.",
                "hide_message" => "Fique quieto. Ele perde voce se nao te vir.",
                _ => key
            };
        }

        private static string English(string key)
        {
            return key switch
            {
                "title" => "LOST OF SILENCE",
                "play" => "PLAY",
                "tutorial" => "TUTORIAL",
                "settings" => "SETTINGS",
                "quit" => "QUIT",
                "back" => "BACK",
                "resume" => "RESUME",
                "restart" => "RESTART",
                "pause" => "PAUSE",
                "victory" => "YOU ESCAPED",
                "victory_body" => "The gate groaned open to the road. The house fell silent behind you.",
                "language" => "LANGUAGE",
                "sensitivity" => "SENSITIVITY",
                "tutorial_body" => "WASD moves | Mouse looks | Shift runs | Ctrl crouches | E interacts | F flashlight | Esc pauses\nOn phone: left side moves, right side looks, large buttons interact and toggle the flashlight.\nFind the key, collect 3 fuses, restore power, reach the yard, and open the outer gate.",
                "start_message" => "You woke up in the bedroom. Find the key to leave.",
                "key_collected" => "Bedroom key collected.",
                "fuse_collected" => "Fuse collected:",
                "missing_fuses" => "Missing fuses:",
                "power_restored" => "Power returned partially. The basement door unlocked.",
                "gate_handle_collected" => "Gate crank collected.",
                "blue_key_collected" => "Blue key collected.",
                "red_key_collected" => "Red key collected.",
                "green_key_collected" => "Green key collected.",
                "gate_missing_handle" => "This gate needs a crank handle.",
                "gate_no_power" => "The mechanism is dead. Restore power first.",
                "gate_opened" => "The outer gate opened.",
                "caught" => "It found you. You wake up in the bedroom again.",
                "objective_key" => "Objective: find the bedroom key",
                "objective_fuses" => "Objective: collect fuses",
                "objective_restore_power" => "Objective: restore power at the fuse box",
                "objective_basement_puzzles" => "Objective: enter the basement and solve the 4 trials",
                "objective_colored_keys" => "Objective: collect the blue, red, and green keys",
                "objective_exit_house" => "Objective: open the main door and find the crank in the yard",
                "objective_escape" => "Objective: leave through the unlocked door",
                "objective_outside_handle" => "Objective: find a crank in the yard",
                "objective_final_gate" => "Objective: open the outer gate",
                "prompt_key" => "E - Take key",
                "prompt_fuse" => "E - Take fuse",
                "prompt_gate_handle" => "E - Take crank",
                "prompt_blue_key" => "E - Take blue key",
                "prompt_red_key" => "E - Take red key",
                "prompt_green_key" => "E - Take green key",
                "prompt_final_gate" => "E - Open gate",
                "prompt_read_note" => "E - Read note",
                "prompt_read_again" => "E - Read again",
                "prompt_keypad" => "E - Use keypad",
                "prompt_valve" => "E - Turn valves",
                "prompt_breaker" => "E - Set breakers",
                "prompt_symbol" => "E - Align symbols",
                "prompt_puzzle" => "E - Solve mechanism",
                "prompt_puzzle_done" => "Solved",
                "prompt_use_stairs" => "E - Use stairs",
                "prompt_use_basement_stairs" => "E - Go to basement",
                "prompt_use_upper_stairs" => "E - Go upstairs",
                "prompt_place_fuses" => "E - Insert fuses",
                "prompt_power_done" => "Power restored",
                "prompt_use_key" => "E - Use key",
                "prompt_locked" => "Locked",
                "prompt_escape" => "E - Escape",
                "prompt_open" => "E - Open door",
                "prompt_close" => "E - Close door",
                "prompt_hide" => "E - Hide",
                "prompt_leave" => "E - Leave",
                "door_unlocked" => "The key turns slowly. The door opened.",
                "door_locked" => "The door is locked.",
                "door_need_blue" => "This door needs the blue key.",
                "door_need_red" => "This door needs the red key.",
                "door_need_green" => "This door needs the green key.",
                "door_need_power" => "No power. Restore the fuses first.",
                "door_need_main_keys" => "The main door has three locks: blue, red, and green.",
                "note_code_message" => "The note says: 'The old safe accepts 413. Do not trust the red lights.'",
                "keypad_start" => "Type the code on the number keys.",
                "keypad_typing" => "Code:",
                "keypad_solved" => "The keypad accepted the code.",
                "keypad_wrong" => "Wrong code. The keypad went dark.",
                "keypad_needs_note" => "You need to read the note first.",
                "puzzle_missing_step" => "A clue is still missing.",
                "valve_solved" => "The valves stopped hissing. Something unlocked.",
                "breaker_solved" => "The breakers clicked into the right order.",
                "symbol_solved" => "The symbols aligned.",
                "puzzle_solved" => "Mechanism solved.",
                "hide_message" => "Stay quiet. It loses you if it cannot see you.",
                _ => key
            };
        }

        private static string Spanish(string key)
        {
            return key switch
            {
                "title" => "LOST OF SILENCE",
                "play" => "JUGAR",
                "tutorial" => "TUTORIAL",
                "settings" => "AJUSTES",
                "quit" => "SALIR",
                "back" => "VOLVER",
                "resume" => "CONTINUAR",
                "restart" => "REINICIAR",
                "pause" => "PAUSA",
                "victory" => "ESCAPASTE",
                "victory_body" => "El porton crujio y se abrio hacia el camino. La casa quedo atras en silencio.",
                "language" => "IDIOMA",
                "sensitivity" => "SENSIBILIDAD",
                "tutorial_body" => "WASD mueve | Mouse mira | Shift corre | Ctrl agacha | E interactua | F linterna | Esc pausa\nEn telefono: lado izquierdo mueve, lado derecho mira, botones grandes interactuan y activan la linterna.\nEncuentra la llave, recoge 3 fusibles, restaura la energia, sal al patio y abre el porton exterior.",
                "start_message" => "Despertaste en el dormitorio. Encuentra la llave para salir.",
                "key_collected" => "Llave del dormitorio recogida.",
                "fuse_collected" => "Fusible recogido:",
                "missing_fuses" => "Faltan fusibles:",
                "power_restored" => "La energia volvio parcialmente. La puerta del sotano se desbloqueo.",
                "gate_handle_collected" => "Manivela del porton recogida.",
                "blue_key_collected" => "Llave azul recogida.",
                "red_key_collected" => "Llave roja recogida.",
                "green_key_collected" => "Llave verde recogida.",
                "gate_missing_handle" => "Falta una manivela para abrir este porton.",
                "gate_no_power" => "El mecanismo esta muerto. Restaura la energia primero.",
                "gate_opened" => "El porton exterior se abrio.",
                "caught" => "Te encontro. Despiertas otra vez en el dormitorio.",
                "objective_key" => "Objetivo: encontrar la llave del dormitorio",
                "objective_fuses" => "Objetivo: recoger fusibles",
                "objective_restore_power" => "Objetivo: restaurar energia en la caja de fusibles",
                "objective_basement_puzzles" => "Objetivo: entra al sotano y resuelve los 4 desafios",
                "objective_colored_keys" => "Objetivo: recoge las llaves azul, roja y verde",
                "objective_exit_house" => "Objetivo: abre la puerta principal y busca la manivela en el patio",
                "objective_escape" => "Objetivo: salir por la puerta desbloqueada",
                "objective_outside_handle" => "Objetivo: busca una manivela en el patio",
                "objective_final_gate" => "Objetivo: abrir el porton exterior",
                "prompt_key" => "E - Tomar llave",
                "prompt_fuse" => "E - Tomar fusible",
                "prompt_gate_handle" => "E - Tomar manivela",
                "prompt_blue_key" => "E - Tomar llave azul",
                "prompt_red_key" => "E - Tomar llave roja",
                "prompt_green_key" => "E - Tomar llave verde",
                "prompt_final_gate" => "E - Abrir porton",
                "prompt_read_note" => "E - Leer nota",
                "prompt_read_again" => "E - Leer otra vez",
                "prompt_keypad" => "E - Usar teclado",
                "prompt_valve" => "E - Girar valvulas",
                "prompt_breaker" => "E - Ajustar interruptores",
                "prompt_symbol" => "E - Alinear simbolos",
                "prompt_puzzle" => "E - Resolver mecanismo",
                "prompt_puzzle_done" => "Resuelto",
                "prompt_use_stairs" => "E - Usar escalera",
                "prompt_use_basement_stairs" => "E - Bajar al sotano",
                "prompt_use_upper_stairs" => "E - Subir al segundo piso",
                "prompt_place_fuses" => "E - Colocar fusibles",
                "prompt_power_done" => "Energia restaurada",
                "prompt_use_key" => "E - Usar llave",
                "prompt_locked" => "Cerrada",
                "prompt_escape" => "E - Escapar",
                "prompt_open" => "E - Abrir puerta",
                "prompt_close" => "E - Cerrar puerta",
                "prompt_hide" => "E - Esconderse",
                "prompt_leave" => "E - Salir",
                "door_unlocked" => "La llave gira despacio. La puerta se abrio.",
                "door_locked" => "La puerta esta cerrada.",
                "door_need_blue" => "Esta puerta necesita la llave azul.",
                "door_need_red" => "Esta puerta necesita la llave roja.",
                "door_need_green" => "Esta puerta necesita la llave verde.",
                "door_need_power" => "No hay energia. Restaura los fusibles primero.",
                "door_need_main_keys" => "La puerta principal tiene tres cerraduras: azul, roja y verde.",
                "note_code_message" => "La nota dice: 'La caja antigua acepta 413. No confies en las luces rojas.'",
                "keypad_start" => "Escribe el codigo con los numeros.",
                "keypad_typing" => "Codigo:",
                "keypad_solved" => "El teclado acepto el codigo.",
                "keypad_wrong" => "Codigo incorrecto. El teclado se apago.",
                "keypad_needs_note" => "Necesitas leer la nota primero.",
                "puzzle_missing_step" => "Todavia falta una pista.",
                "valve_solved" => "Las valvulas dejaron de silbar. Algo se desbloqueo.",
                "breaker_solved" => "Los interruptores encajaron en el orden correcto.",
                "symbol_solved" => "Los simbolos se alinearon.",
                "puzzle_solved" => "Mecanismo resuelto.",
                "hide_message" => "Quedate quieto. Te pierde si no puede verte.",
                _ => key
            };
        }
    }
}
