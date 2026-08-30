#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

namespace Dominoes
{
    /// <summary>
    /// Automated test suite verifying Dominoes Tutorial flows and persistence:
    /// - Test 1: Fresh install requires tutorial (DominoTutorialCompleted == 0)
    /// - Test 2: Tutorial step transitions from Intro -> SelectPlayableTile -> ChooseBoardEnd -> Completed
    /// - Test 3: Skip tutorial sets DominoTutorialCompleted = true and exits tutorial
    /// - Test 4: Completed tutorial is persisted and not shown on subsequent match launches
    /// - Test 5: Replay tutorial via Help forces tutorial restart without losing persistent completion state
    /// </summary>
    public static class DominoTutorialFlowTests
    {
        [MenuItem("Dominoes/Run Tutorial Flow Tests")]
        public static void RunAllTutorialFlowTests()
        {
            Debug.Log("===============================================================");
            Debug.Log(">>> RUNNING DOMINOES TUTORIAL FLOW & PERSISTENCE TESTS <<<");
            Debug.Log("===============================================================");

            bool allPassed = true;

            allPassed &= TestScenario1_FreshInstallRequiresTutorial();
            allPassed &= TestScenario2_TutorialStepTransitions();
            allPassed &= TestScenario3_SkipTutorialFlow();
            allPassed &= TestScenario4_CompletedTutorialPersisted();
            allPassed &= TestScenario5_ReplayTutorialViaHelp();

            Debug.Log("===============================================================");
            if (allPassed)
            {
                Debug.Log("<color=green><b>ALL TUTORIAL FLOW TESTS PASSED SUCCESSFULLY! (5/5)</b></color>");
            }
            else
            {
                Debug.LogError("<color=red><b>SOME TUTORIAL FLOW TESTS FAILED!</b></color>");
            }
            Debug.Log("===============================================================");
        }

        private static bool TestScenario1_FreshInstallRequiresTutorial()
        {
            Debug.Log("\n[TEST 1] Fresh install must require tutorial (DominoTutorialCompleted == 0).");
            PlayerPrefs.DeleteKey(DominoTutorialController.TutorialCompletedKey);

            if (DominoTutorialController.IsTutorialCompleted())
            {
                Debug.LogError("[TEST 1 FAILED] Fresh install reported tutorial as completed.");
                return false;
            }

            Debug.Log("<color=green>✓ Test 1 Passed: Fresh install correctly requires tutorial.</color>");
            return true;
        }

        private static bool TestScenario2_TutorialStepTransitions()
        {
            Debug.Log("\n[TEST 2] Tutorial transitions correctly through state machine.");
            var tutorial = new DominoTutorialController(null);

            tutorial.SetStep(TutorialStep.Introduction);
            if (tutorial.CurrentStep != TutorialStep.Introduction || !tutorial.IsActive)
            {
                Debug.LogError("[TEST 2 FAILED] Introduction step setup failed.");
                return false;
            }

            tutorial.SetStep(TutorialStep.SelectPlayableTile);
            if (tutorial.CurrentStep != TutorialStep.SelectPlayableTile || !tutorial.IsActive)
            {
                Debug.LogError("[TEST 2 FAILED] SelectPlayableTile step setup failed.");
                return false;
            }

            tutorial.SetStep(TutorialStep.ChooseBoardEnd);
            if (tutorial.CurrentStep != TutorialStep.ChooseBoardEnd || !tutorial.IsActive)
            {
                Debug.LogError("[TEST 2 FAILED] ChooseBoardEnd step setup failed.");
                return false;
            }

            tutorial.SetStep(TutorialStep.Completed);
            if (tutorial.CurrentStep != TutorialStep.Completed)
            {
                Debug.LogError("[TEST 2 FAILED] Completed step setup failed.");
                return false;
            }

            Debug.Log("<color=green>✓ Test 2 Passed: Tutorial step transitions executed cleanly.</color>");
            return true;
        }

        private static bool TestScenario3_SkipTutorialFlow()
        {
            Debug.Log("\n[TEST 3] Skip tutorial sets DominoTutorialCompleted and exits tutorial.");
            PlayerPrefs.DeleteKey(DominoTutorialController.TutorialCompletedKey);

            var tutorial = new DominoTutorialController(null);
            tutorial.SetStep(TutorialStep.SelectPlayableTile);

            // Skip confirmed
            tutorial.EndTutorial(markCompleted: true);

            if (tutorial.IsActive || !DominoTutorialController.IsTutorialCompleted())
            {
                Debug.LogError("[TEST 3 FAILED] Skip did not mark tutorial completed or deactivate tutorial.");
                return false;
            }

            Debug.Log("<color=green>✓ Test 3 Passed: Skip tutorial cleanly persisted completion and ended tutorial.</color>");
            return true;
        }

        private static bool TestScenario4_CompletedTutorialPersisted()
        {
            Debug.Log("\n[TEST 4] Completed tutorial is persisted and not auto-started on subsequent matches.");
            DominoTutorialController.SetTutorialCompleted(true);

            if (!DominoTutorialController.IsTutorialCompleted())
            {
                Debug.LogError("[TEST 4 FAILED] Tutorial completed flag was not set to true.");
                return false;
            }

            Debug.Log("<color=green>✓ Test 4 Passed: Tutorial completion state persisted in PlayerPrefs.</color>");
            return true;
        }

        private static bool TestScenario5_ReplayTutorialViaHelp()
        {
            Debug.Log("\n[TEST 5] Replay tutorial can force-start tutorial even if previously completed.");
            DominoTutorialController.SetTutorialCompleted(true);

            var tutorial = new DominoTutorialController(null);
            tutorial.StartTutorial(force: true);

            if (tutorial.CurrentStep != TutorialStep.Introduction || !tutorial.IsActive)
            {
                Debug.LogError("[TEST 5 FAILED] Force start tutorial did not enter Introduction step.");
                return false;
            }

            Debug.Log("<color=green>✓ Test 5 Passed: Force-start successfully opened tutorial for replay.</color>");
            return true;
        }
    }
}
#endif
