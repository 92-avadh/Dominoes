using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Dominoes
{
    /// <summary>
    /// Tutorial state machine steps for the interactive first-time playable tutorial.
    /// </summary>
    public enum TutorialStep
    {
        None,
        Introduction,
        SelectPlayableTile,
        ChooseBoardEnd,
        DrawTile,
        PassTurn,
        WatchBotTurn,
        ChooseEitherEnd,
        MiniRound,
        Completed
    }

    /// <summary>
    /// Dedicated presentation and guidance controller for the interactive first-time Dominoes tutorial.
    /// Governs tutorial state transitions, input gating, visual spotlights, animated pointers, non-blocking hints, and skip flow.
    /// Does not own or duplicate source-of-truth game state.
    /// </summary>
    public class DominoTutorialController
    {
        public const string TutorialCompletedKey = "DominoTutorialCompleted";

        private readonly DominoGameScreenUIToolkitController gameScreenController;
        private TutorialStep currentStep = TutorialStep.None;

        // UI Element References
        private VisualElement tutorialOverlay;
        private VisualElement tutorialBackdrop;
        private VisualElement tutorialCard;
        private Label tutorialEyebrow;
        private Label tutorialTitle;
        private Label tutorialDesc;
        private Button tutorialPrimaryBtn;
        private Button tutorialSkipBtn;
        private VisualElement tutorialPointer;
        private VisualElement tutorialHintToast;
        private Label tutorialHintText;
        private VisualElement tutorialSkipModal;
        private Button skipContinueBtn;
        private Button skipConfirmBtn;

        // Step State Cache
        private DominoTile targetPlayableTile;
        private Coroutine hintCoroutine;

        public TutorialStep CurrentStep => currentStep;
        public bool IsActive => currentStep != TutorialStep.None && currentStep != TutorialStep.Completed;
        public DominoTile TargetPlayableTile => targetPlayableTile;

        public event Action<TutorialStep> OnStepChanged;

        public DominoTutorialController(DominoGameScreenUIToolkitController gameScreenController)
        {
            this.gameScreenController = gameScreenController;
        }

        public static bool IsTutorialCompleted()
        {
            return PlayerPrefs.GetInt(TutorialCompletedKey, 0) == 1;
        }

        public static void SetTutorialCompleted(bool completed)
        {
            PlayerPrefs.SetInt(TutorialCompletedKey, completed ? 1 : 0);
            PlayerPrefs.Save();
        }

        public static void ResetTutorial()
        {
            SetTutorialCompleted(false);
        }

        private VisualElement tutorialMascotOverlay;
        private Label tutorialMascotText;
        private VisualElement tutorialFingerPointer;

        public void BindUIElements(VisualElement root)
        {
            if (root == null) return;

            tutorialOverlay = root.Q<VisualElement>("tutorial-overlay");
            tutorialBackdrop = root.Q<VisualElement>("tutorial-backdrop");
            tutorialCard = root.Q<VisualElement>("tutorial-card");
            tutorialEyebrow = root.Q<Label>("tutorial-eyebrow");
            tutorialTitle = root.Q<Label>("tutorial-title");
            tutorialDesc = root.Q<Label>("tutorial-desc");
            tutorialPrimaryBtn = root.Q<Button>("tutorial-primary-btn");
            tutorialSkipBtn = root.Q<Button>("tutorial-skip-btn");
            tutorialPointer = root.Q<VisualElement>("tutorial-pointer");
            tutorialHintToast = root.Q<VisualElement>("tutorial-hint-toast");
            tutorialHintText = root.Q<Label>("tutorial-hint-text");
            tutorialSkipModal = root.Q<VisualElement>("tutorial-skip-modal");
            skipContinueBtn = root.Q<Button>("skip-continue-btn");
            skipConfirmBtn = root.Q<Button>("skip-confirm-btn");

            // Mascot & Speech Bubble
            tutorialMascotOverlay = root.Q<VisualElement>("tutorial-mascot-overlay");
            tutorialMascotText = root.Q<Label>("tutorial-mascot-text");
            tutorialFingerPointer = root.Q<VisualElement>("tutorial-finger-pointer");

            if (tutorialOverlay != null) tutorialOverlay.pickingMode = PickingMode.Ignore;
            if (tutorialPointer != null) tutorialPointer.pickingMode = PickingMode.Ignore;
            if (tutorialHintToast != null) tutorialHintToast.pickingMode = PickingMode.Ignore;
            if (tutorialFingerPointer != null) tutorialFingerPointer.pickingMode = PickingMode.Ignore;

            if (tutorialPrimaryBtn != null) tutorialPrimaryBtn.clicked += OnPrimaryButtonClicked;
            if (tutorialSkipBtn != null) tutorialSkipBtn.clicked += OnSkipButtonClicked;
            if (skipContinueBtn != null) skipContinueBtn.clicked += OnCancelSkipClicked;
            if (skipConfirmBtn != null) skipConfirmBtn.clicked += OnConfirmSkipClicked;
        }

        public void UnbindUIElements()
        {
            if (tutorialPrimaryBtn != null) tutorialPrimaryBtn.clicked -= OnPrimaryButtonClicked;
            if (tutorialSkipBtn != null) tutorialSkipBtn.clicked -= OnSkipButtonClicked;
            if (skipContinueBtn != null) skipContinueBtn.clicked -= OnCancelSkipClicked;
            if (skipConfirmBtn != null) skipConfirmBtn.clicked -= OnConfirmSkipClicked;

            tutorialOverlay = null;
            tutorialBackdrop = null;
            tutorialCard = null;
            tutorialEyebrow = null;
            tutorialTitle = null;
            tutorialDesc = null;
            tutorialPrimaryBtn = null;
            tutorialSkipBtn = null;
            tutorialPointer = null;
            tutorialHintToast = null;
            tutorialMascotOverlay = null;
            tutorialMascotText = null;
            tutorialFingerPointer = null;
            tutorialHintText = null;
            tutorialSkipModal = null;
            skipContinueBtn = null;
            skipConfirmBtn = null;
        }

        /// <summary>
        /// Starts the interactive tutorial if not already completed or forced.
        /// </summary>
        public void StartTutorial(bool force = false)
        {
            if (!force && IsTutorialCompleted())
            {
                EndTutorial(markCompleted: true);
                return;
            }

            SetStep(TutorialStep.Introduction);
        }

        /// <summary>
        /// Advances to a specific tutorial step and updates UI state.
        /// </summary>
        public void SetStep(TutorialStep newStep)
        {
            currentStep = newStep;
            Debug.Log($"<color=cyan>[DominoTutorialController] Step changed to: {currentStep}</color>");

            HideHint();
            if (tutorialSkipModal != null) tutorialSkipModal.style.display = DisplayStyle.None;

            switch (currentStep)
            {
                case TutorialStep.Introduction:
                    ShowIntroduction();
                    break;

                case TutorialStep.SelectPlayableTile:
                    SetupSelectPlayableTileStep();
                    break;

                case TutorialStep.ChooseBoardEnd:
                    SetupChooseBoardEndStep();
                    break;

                case TutorialStep.DrawTile:
                    SetupDrawTileStep();
                    break;

                case TutorialStep.PassTurn:
                    SetupPassTurnStep();
                    break;

                case TutorialStep.WatchBotTurn:
                    SetupWatchBotTurnStep();
                    break;

                case TutorialStep.ChooseEitherEnd:
                    SetupChooseEitherEndStep();
                    break;

                case TutorialStep.MiniRound:
                    SetupMiniRoundStep();
                    break;

                case TutorialStep.Completed:
                    ShowCompletion();
                    break;

                case TutorialStep.None:
                default:
                    HideAllTutorialUI();
                    break;
            }

            OnStepChanged?.Invoke(currentStep);
            gameScreenController?.RefreshAll();
        }

        #region Step Setups

        private void ShowIntroduction()
        {
            if (tutorialOverlay != null) tutorialOverlay.style.display = DisplayStyle.Flex;
            if (tutorialBackdrop != null) tutorialBackdrop.style.display = DisplayStyle.Flex;
            if (tutorialCard != null) tutorialCard.style.display = DisplayStyle.Flex;
            if (tutorialPointer != null) tutorialPointer.style.display = DisplayStyle.None;

            if (tutorialEyebrow != null) tutorialEyebrow.text = "• INTERACTIVE GUIDE •";
            if (tutorialTitle != null) tutorialTitle.text = "HOW TO PLAY DOMINOES";
            if (tutorialDesc != null)
            {
                tutorialDesc.text = "Match the number on one side of your tile with an open end on the board.\n\nYou will learn by playing a short interactive match!";
            }
            if (tutorialPrimaryBtn != null)
            {
                tutorialPrimaryBtn.text = "LET'S PLAY";
                tutorialPrimaryBtn.style.display = DisplayStyle.Flex;
            }
            if (tutorialSkipBtn != null) tutorialSkipBtn.style.display = DisplayStyle.Flex;

            gameScreenController?.SetInstruction("Tap LET'S PLAY to begin the interactive tutorial.");
        }

        private void SetupSelectPlayableTileStep()
        {
            if (tutorialOverlay != null) tutorialOverlay.style.display = DisplayStyle.Flex;
            if (tutorialBackdrop != null) tutorialBackdrop.style.display = DisplayStyle.None;
            if (tutorialCard != null) tutorialCard.style.display = DisplayStyle.None;
            if (tutorialSkipBtn != null) tutorialSkipBtn.style.display = DisplayStyle.Flex;

            // Show Mascot Dialogue Bubble (Screenshot 1)
            if (tutorialMascotOverlay != null) tutorialMascotOverlay.style.display = DisplayStyle.Flex;
            if (tutorialMascotText != null) tutorialMascotText.text = "Let's learn Domino. Start by placing a tile.";
            if (tutorialFingerPointer != null) tutorialFingerPointer.style.display = DisplayStyle.Flex;

            FindTargetPlayableTile();

            if (tutorialPointer != null)
            {
                tutorialPointer.style.display = DisplayStyle.Flex;
            }

            if (targetPlayableTile != null)
            {
                gameScreenController?.SetInstruction($"YOUR TURN • Drag [{targetPlayableTile.Left}|{targetPlayableTile.Right}] from your hand onto the board");
            }
            else
            {
                gameScreenController?.SetInstruction("YOUR TURN • Drag a highlighted domino from your hand onto the board");
            }
        }

        private void SetupChooseBoardEndStep()
        {
            if (tutorialOverlay != null) tutorialOverlay.style.display = DisplayStyle.Flex;
            if (tutorialBackdrop != null) tutorialBackdrop.style.display = DisplayStyle.None;
            if (tutorialCard != null) tutorialCard.style.display = DisplayStyle.None;
            if (tutorialPointer != null) tutorialPointer.style.display = DisplayStyle.None;
            if (tutorialFingerPointer != null) tutorialFingerPointer.style.display = DisplayStyle.None;
            if (tutorialSkipBtn != null) tutorialSkipBtn.style.display = DisplayStyle.Flex;

            if (tutorialMascotText != null) tutorialMascotText.text = "Tap the matching endpoint to place your domino!";

            if (targetPlayableTile != null && gameScreenController != null && gameScreenController.SelectedTile == null)
            {
                gameScreenController.SelectTile(targetPlayableTile);
            }

            gameScreenController?.SetInstruction("Tap the matching open endpoint marker on the board.");
        }

        private void SetupDrawTileStep()
        {
            if (tutorialOverlay != null) tutorialOverlay.style.display = DisplayStyle.Flex;
            if (tutorialBackdrop != null) tutorialBackdrop.style.display = DisplayStyle.None;
            if (tutorialCard != null) tutorialCard.style.display = DisplayStyle.None;
            if (tutorialPointer != null) tutorialPointer.style.display = DisplayStyle.None;
            if (tutorialSkipBtn != null) tutorialSkipBtn.style.display = DisplayStyle.Flex;

            if (tutorialMascotOverlay != null) tutorialMascotOverlay.style.display = DisplayStyle.Flex;
            if (tutorialMascotText != null) tutorialMascotText.text = "No moves! Tap the Boneyard to draw a new tile.";

            gameScreenController?.SetInstruction("No matching dominoes! Tap the Boneyard stack to draw a new tile.");
        }

        private void SetupPassTurnStep()
        {
            if (tutorialOverlay != null) tutorialOverlay.style.display = DisplayStyle.Flex;
            if (tutorialBackdrop != null) tutorialBackdrop.style.display = DisplayStyle.None;
            if (tutorialCard != null) tutorialCard.style.display = DisplayStyle.None;
            if (tutorialPointer != null) tutorialPointer.style.display = DisplayStyle.Flex;
            if (tutorialSkipBtn != null) tutorialSkipBtn.style.display = DisplayStyle.Flex;

            gameScreenController?.SetInstruction("YOUR TURN • No moves and boneyard empty. Tap PASS TURN to continue");
        }

        private void SetupWatchBotTurnStep()
        {
            if (tutorialOverlay != null) tutorialOverlay.style.display = DisplayStyle.Flex;
            if (tutorialBackdrop != null) tutorialBackdrop.style.display = DisplayStyle.None;
            if (tutorialCard != null) tutorialCard.style.display = DisplayStyle.None;
            if (tutorialPointer != null) tutorialPointer.style.display = DisplayStyle.None;
            if (tutorialSkipBtn != null) tutorialSkipBtn.style.display = DisplayStyle.Flex;

            gameScreenController?.SetInstruction("SOPHIA'S TURN • Sophia is thinking...");
        }

        private void SetupChooseEitherEndStep()
        {
            if (tutorialOverlay != null) tutorialOverlay.style.display = DisplayStyle.Flex;
            if (tutorialBackdrop != null) tutorialBackdrop.style.display = DisplayStyle.None;
            if (tutorialCard != null) tutorialCard.style.display = DisplayStyle.None;
            if (tutorialPointer != null) tutorialPointer.style.display = DisplayStyle.None;
            if (tutorialSkipBtn != null) tutorialSkipBtn.style.display = DisplayStyle.Flex;

            FindTargetPlayableTile();
            gameScreenController?.SetInstruction("You can play on either open end! Tap a highlighted domino, then tap either end");
        }

        private void SetupMiniRoundStep()
        {
            if (tutorialOverlay != null) tutorialOverlay.style.display = DisplayStyle.None;
            if (tutorialBackdrop != null) tutorialBackdrop.style.display = DisplayStyle.None;
            if (tutorialCard != null) tutorialCard.style.display = DisplayStyle.None;
            if (tutorialPointer != null) tutorialPointer.style.display = DisplayStyle.None;

            gameScreenController?.SetInstruction("GREAT! Play dominoes that match either end of the board");
        }

        private void ShowCompletion()
        {
            SetTutorialCompleted(true);
            DominoAudioManager.Instance?.PlayWin();
            DominoHapticsManager.TriggerWinCelebration();

            if (tutorialOverlay != null) tutorialOverlay.style.display = DisplayStyle.Flex;
            if (tutorialBackdrop != null) tutorialBackdrop.style.display = DisplayStyle.Flex;
            if (tutorialCard != null) tutorialCard.style.display = DisplayStyle.Flex;
            if (tutorialPointer != null) tutorialPointer.style.display = DisplayStyle.None;
            if (tutorialSkipBtn != null) tutorialSkipBtn.style.display = DisplayStyle.None;

            if (tutorialEyebrow != null) tutorialEyebrow.text = "• TUTORIAL COMPLETE •";
            if (tutorialTitle != null) tutorialTitle.text = "YOU'RE READY! 🎉";
            if (tutorialDesc != null)
            {
                tutorialDesc.text = "You know the basics of Dominoes!\n\n• Match numbers to open board ends\n• Play on either left or right end\n• Draw when you have no matching tile\n• Pass when no moves are possible";
            }
            if (tutorialPrimaryBtn != null)
            {
                tutorialPrimaryBtn.text = "PLAY FOR REAL";
                tutorialPrimaryBtn.style.display = DisplayStyle.Flex;
            }

            gameScreenController?.SetInstruction("Tutorial Complete! Tap PLAY FOR REAL to continue.");
        }

        #endregion

        #region Input Interception & Guidance Hooks

        /// <summary>
        /// Intercepts hand tile selection during tutorial.
        /// Returns true if selection is permitted, false if blocked.
        /// </summary>
        public bool InterceptHandTileClick(DominoTile tile, bool isPlayable)
        {
            if (!IsActive) return true;

            if (currentStep == TutorialStep.SelectPlayableTile)
            {
                if (targetPlayableTile != null && tile == targetPlayableTile)
                {
                    // Target tile tapped! Proceed to choose board end.
                    SetStep(TutorialStep.ChooseBoardEnd);
                    return true;
                }
                else if (isPlayable)
                {
                    // Any other playable tile
                    targetPlayableTile = tile;
                    SetStep(TutorialStep.ChooseBoardEnd);
                    return true;
                }
                else
                {
                    ShowHint("Try the highlighted domino!");
                    return false;
                }
            }
            else if (currentStep == TutorialStep.ChooseBoardEnd)
            {
                if (isPlayable)
                {
                    targetPlayableTile = tile;
                    return true;
                }
                else
                {
                    ShowHint("Pick a highlighted matching tile or tap a board end.");
                    return false;
                }
            }
            else if (currentStep == TutorialStep.ChooseEitherEnd || currentStep == TutorialStep.MiniRound)
            {
                if (isPlayable)
                {
                    return true;
                }
                else
                {
                    ShowHint("Tile does not match either end of the board.");
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Intercepts endpoint placement during tutorial.
        /// Returns true if permitted.
        /// </summary>
        public bool InterceptEndpointClick(BoardSide side, bool canConnect)
        {
            if (!IsActive) return true;

            if (currentStep == TutorialStep.ChooseBoardEnd)
            {
                if (canConnect)
                {
                    // Valid endpoint tapped! Placement will succeed.
                    ShowHint("Nice move!");
                    if (gameScreenController != null)
                    {
                        gameScreenController.StartCoroutine(DelayedStepTransition(TutorialStep.WatchBotTurn, 0.4f));
                    }
                    return true;
                }
                else
                {
                    ShowHint("Select an active endpoint target!");
                    return false;
                }
            }
            else if (currentStep == TutorialStep.ChooseEitherEnd)
            {
                if (canConnect)
                {
                    ShowHint("Great placement!");
                    if (gameScreenController != null)
                    {
                        gameScreenController.StartCoroutine(DelayedStepTransition(TutorialStep.MiniRound, 0.4f));
                    }
                    return true;
                }
                else
                {
                    ShowHint("Select an active endpoint target!");
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Intercepts Draw button click during tutorial.
        /// </summary>
        public bool InterceptDrawClick()
        {
            if (!IsActive) return true;

            if (currentStep == TutorialStep.DrawTile)
            {
                ShowHint("You drew a tile! If it matches the board, tap it to play.");
                SetStep(TutorialStep.SelectPlayableTile);
                return true;
            }

            return true;
        }

        /// <summary>
        /// Intercepts Pass button click during tutorial.
        /// </summary>
        public bool InterceptPassClick()
        {
            if (!IsActive) return true;

            if (currentStep == TutorialStep.PassTurn)
            {
                ShowHint("Passing your turn.");
                SetStep(TutorialStep.WatchBotTurn);
                return true;
            }

            return true;
        }

        /// <summary>
        /// Notification from GameScreenController when a bot finishes its turn.
        /// </summary>
        public void NotifyBotTurnEnded()
        {
            if (!IsActive) return;

            if (currentStep == TutorialStep.WatchBotTurn)
            {
                // Bot played! Return to player turn with either/both ends guidance or mini round
                SetStep(TutorialStep.ChooseEitherEnd);
            }
        }

        /// <summary>
        /// Notification from GameScreenController when human has played multiple moves in MiniRound.
        /// </summary>
        public void NotifyMoveExecuted()
        {
            if (!IsActive) return;

            if (currentStep == TutorialStep.MiniRound)
            {
                // Check if tutorial mini-round complete
                SetStep(TutorialStep.Completed);
            }
        }

        #endregion

        #region Helper Methods

        private void FindTargetPlayableTile()
        {
            targetPlayableTile = null;
            if (gameScreenController == null || gameScreenController.UIDocument == null) return;

            var match = gameScreenController.CurrentMatchManager;
            if (match == null) return;

            var human = match.TurnManager.GetCurrentPlayer(match.GameState.Players);
            if (human == null || !human.IsHuman) return;

            // Find first playable tile in hand
            foreach (var tile in human.Hand)
            {
                if (DominoMoveValidator.CanPlaceAnywhere(match.Board, tile, out _))
                {
                    targetPlayableTile = tile;
                    break;
                }
            }
        }

        public void ShowHint(string message)
        {
            if (tutorialHintToast != null && tutorialHintText != null)
            {
                tutorialHintText.text = message;
                tutorialHintToast.style.display = DisplayStyle.Flex;

                if (gameScreenController != null)
                {
                    if (hintCoroutine != null) gameScreenController.StopCoroutine(hintCoroutine);
                    hintCoroutine = gameScreenController.StartCoroutine(HideHintAfterDelay(2.5f));
                }
            }
        }

        public void HideHint()
        {
            if (tutorialHintToast != null)
            {
                tutorialHintToast.style.display = DisplayStyle.None;
            }
        }

        private IEnumerator HideHintAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            HideHint();
            hintCoroutine = null;
        }

        private IEnumerator DelayedStepTransition(TutorialStep step, float delay)
        {
            yield return new WaitForSeconds(delay);
            SetStep(step);
        }

        private void HideAllTutorialUI()
        {
            if (tutorialOverlay != null) tutorialOverlay.style.display = DisplayStyle.None;
            if (tutorialBackdrop != null) tutorialBackdrop.style.display = DisplayStyle.None;
            if (tutorialCard != null) tutorialCard.style.display = DisplayStyle.None;
            if (tutorialPointer != null) tutorialPointer.style.display = DisplayStyle.None;
            if (tutorialHintToast != null) tutorialHintToast.style.display = DisplayStyle.None;
            if (tutorialSkipModal != null) tutorialSkipModal.style.display = DisplayStyle.None;
            if (tutorialMascotOverlay != null) tutorialMascotOverlay.style.display = DisplayStyle.None;
            if (tutorialFingerPointer != null) tutorialFingerPointer.style.display = DisplayStyle.None;
        }

        public void EndTutorial(bool markCompleted)
        {
            if (markCompleted)
            {
                SetTutorialCompleted(true);
            }

            currentStep = TutorialStep.None;
            HideAllTutorialUI();
            gameScreenController?.RefreshAll();
        }

        #endregion

        #region Button Handlers

        private void OnPrimaryButtonClicked()
        {
            if (currentStep == TutorialStep.Introduction)
            {
                SetStep(TutorialStep.SelectPlayableTile);
            }
            else if (currentStep == TutorialStep.Completed)
            {
                EndTutorial(markCompleted: true);
            }
        }

        private void OnSkipButtonClicked()
        {
            if (tutorialSkipModal != null)
            {
                tutorialSkipModal.style.display = DisplayStyle.Flex;
            }
            if (tutorialBackdrop != null)
            {
                tutorialBackdrop.style.display = DisplayStyle.Flex;
            }
        }

        private void OnCancelSkipClicked()
        {
            if (tutorialSkipModal != null)
            {
                tutorialSkipModal.style.display = DisplayStyle.None;
            }
            if (tutorialBackdrop != null && currentStep != TutorialStep.Introduction && currentStep != TutorialStep.Completed)
            {
                tutorialBackdrop.style.display = DisplayStyle.None;
            }
        }

        private void OnConfirmSkipClicked()
        {
            EndTutorial(markCompleted: true);
        }

        #endregion
    }
}
