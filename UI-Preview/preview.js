/**
 * preview.js - Interactive Controller for Unity Dominoes Screenshot Matching Sandbox
 */

document.addEventListener('DOMContentLoaded', () => {
    // 1. Viewport Size Switcher
    const deviceFrame = document.getElementById('device-frame');
    const presetButtons = document.querySelectorAll('#viewport-switcher .preset-btn');

    presetButtons.forEach(btn => {
        btn.addEventListener('click', () => {
            presetButtons.forEach(b => b.classList.remove('active'));
            btn.classList.add('active');

            const width = btn.getAttribute('data-width');
            const height = btn.getAttribute('data-height');

            if (deviceFrame && width && height) {
                deviceFrame.style.width = `${width}px`;
                deviceFrame.style.height = `${height}px`;
            }
        });
    });

    // 2. Screen Switcher
    const screenButtons = document.querySelectorAll('#screen-switcher .screen-btn');
    const uiScreens = document.querySelectorAll('.ui-screen');

    function navigateToScreen(screenId) {
        screenButtons.forEach(b => {
            if (b.getAttribute('data-screen') === screenId) {
                b.classList.add('active');
            } else {
                b.classList.remove('active');
            }
        });

        uiScreens.forEach(screen => {
            if (screen.id === `screen-${screenId}`) {
                screen.style.display = 'flex';
                void screen.offsetWidth;
                screen.classList.add('active');
            } else {
                screen.classList.remove('active');
                setTimeout(() => {
                    if (!screen.classList.contains('active')) {
                        screen.style.display = 'none';
                    }
                }, 300);
            }
        });
    }

    screenButtons.forEach(btn => {
        btn.addEventListener('click', () => {
            const screenId = btn.getAttribute('data-screen');
            if (screenId) {
                navigateToScreen(screenId);
            }
        });
    });

    // 3. Interactive In-App Navigation Triggers
    const modeOnlineBtn = document.getElementById('mode-online-btn');
    const modeComputerBtn = document.getElementById('mode-computer-btn');
    const modeFriendsBtn = document.getElementById('mode-friends-btn');
    const leaveButton = document.getElementById('leave-button');
    const leaveConfirmModal = document.getElementById('leave-confirm-modal');
    const leaveModalConfirmBtn = document.getElementById('leave-modal-confirm-btn');
    const leaveModalCancelBtn = document.getElementById('leave-modal-cancel-btn');
    const alertCloseXBtn = document.getElementById('alert-close-x-btn');
    const gameMenuBtn = document.getElementById('game-menu-btn');
    const homeHamburgerBtn = document.getElementById('home-hamburger-btn');
    const homeProfileBtn = document.getElementById('home-profile-btn');
    const profileCloseBtn = document.getElementById('profile-close-btn');
    const homeSettingsCloseBtn = document.getElementById('home-settings-close-btn');
    const homeSettingsModal = document.getElementById('home-settings-modal');
    const profileStatsModal = document.getElementById('profile-stats-modal');

    // Sub-Modals
    const computerDifficultyModal = document.getElementById('computer-difficulty-modal');
    const computerModalCloseBtn = document.getElementById('computer-modal-close-btn');
    const diffEasyBtn = document.getElementById('diff-easy-btn');
    const diffMediumBtn = document.getElementById('diff-medium-btn');
    const diffHardBtn = document.getElementById('diff-hard-btn');

    const onlineRulesModal = document.getElementById('online-rules-modal');
    const onlineModalCloseBtn = document.getElementById('online-modal-close-btn');
    const onlineFindMatchBtn = document.getElementById('online-find-match-btn');
    const ruleCards = document.querySelectorAll('.rules-grid .rule-card');

    const friendRoomModal = document.getElementById('friend-room-modal');
    const friendModalCloseBtn = document.getElementById('friend-modal-close-btn');
    const previewTabCreate = document.getElementById('preview-tab-create');
    const previewTabJoin = document.getElementById('preview-tab-join');
    const previewCreatePanel = document.getElementById('preview-create-panel');
    const previewJoinPanel = document.getElementById('preview-join-panel');
    const previewCopyCodeBtn = document.getElementById('preview-copy-code-btn');
    const previewGeneratedCode = document.getElementById('preview-generated-code');
    const previewStartHostBtn = document.getElementById('preview-start-host-btn');
    const previewJoinRoomBtn = document.getElementById('preview-join-room-btn');
    const previewRoomInput = document.getElementById('preview-room-input');
    const previewJoinStatus = document.getElementById('preview-join-status');

    function hideAllHomeModals() {
        if (computerDifficultyModal) computerDifficultyModal.classList.add('hidden');
        if (onlineRulesModal) onlineRulesModal.classList.add('hidden');
        if (friendRoomModal) friendRoomModal.classList.add('hidden');
        if (profileStatsModal) profileStatsModal.classList.add('hidden');
        if (homeSettingsModal) homeSettingsModal.classList.add('hidden');
    }

    // VS Computer Trigger
    if (modeComputerBtn) {
        modeComputerBtn.addEventListener('click', () => {
            hideAllHomeModals();
            if (computerDifficultyModal) computerDifficultyModal.classList.remove('hidden');
        });
    }
    if (computerModalCloseBtn) {
        computerModalCloseBtn.addEventListener('click', hideAllHomeModals);
    }
    [diffEasyBtn, diffMediumBtn, diffHardBtn].forEach(btn => {
        if (btn) {
            btn.addEventListener('click', () => {
                hideAllHomeModals();
                navigateToScreen('game');
            });
        }
    });

    // Online Trigger
    if (modeOnlineBtn) {
        modeOnlineBtn.addEventListener('click', () => {
            hideAllHomeModals();
            if (onlineRulesModal) onlineRulesModal.classList.remove('hidden');
        });
    }
    ruleCards.forEach(card => {
        card.addEventListener('click', () => {
            ruleCards.forEach(c => c.classList.remove('rule-card--active'));
            card.classList.add('rule-card--active');
        });
    });
    if (onlineFindMatchBtn) {
        onlineFindMatchBtn.addEventListener('click', () => {
            hideAllHomeModals();
            navigateToScreen('waiting');
        });
    }
    if (onlineModalCloseBtn) {
        onlineModalCloseBtn.addEventListener('click', hideAllHomeModals);
    }

    // Friend Room Trigger
    if (modeFriendsBtn) {
        modeFriendsBtn.addEventListener('click', () => {
            hideAllHomeModals();
            const randCode = Math.floor(1000 + Math.random() * 9000);
            if (previewGeneratedCode) previewGeneratedCode.textContent = `DOM-${randCode}`;
            if (previewCopyCodeBtn) previewCopyCodeBtn.textContent = '📋 COPY ROOM CODE';
            if (previewTabCreate) previewTabCreate.classList.add('friend-tab-btn--active');
            if (previewTabJoin) previewTabJoin.classList.remove('friend-tab-btn--active');
            if (previewCreatePanel) previewCreatePanel.classList.remove('hidden');
            if (previewJoinPanel) previewJoinPanel.classList.add('hidden');
            if (friendRoomModal) friendRoomModal.classList.remove('hidden');
        });
    }
    if (previewTabCreate && previewTabJoin) {
        previewTabCreate.addEventListener('click', () => {
            previewTabCreate.classList.add('friend-tab-btn--active');
            previewTabJoin.classList.remove('friend-tab-btn--active');
            if (previewCreatePanel) previewCreatePanel.classList.remove('hidden');
            if (previewJoinPanel) previewJoinPanel.classList.add('hidden');
        });
        previewTabJoin.addEventListener('click', () => {
            previewTabJoin.classList.add('friend-tab-btn--active');
            previewTabCreate.classList.remove('friend-tab-btn--active');
            if (previewJoinPanel) previewJoinPanel.classList.remove('hidden');
            if (previewCreatePanel) previewCreatePanel.classList.add('hidden');
        });
    }
    if (previewCopyCodeBtn && previewGeneratedCode) {
        previewCopyCodeBtn.addEventListener('click', () => {
            navigator.clipboard?.writeText(previewGeneratedCode.textContent);
            previewCopyCodeBtn.textContent = '✅ COPIED TO CLIPBOARD!';
        });
    }
    if (previewStartHostBtn) {
        previewStartHostBtn.addEventListener('click', () => {
            hideAllHomeModals();
            navigateToScreen('waiting');
        });
    }
    if (previewJoinRoomBtn) {
        previewJoinRoomBtn.addEventListener('click', () => {
            const val = previewRoomInput ? previewRoomInput.value.trim() : '';
            if (!val) {
                if (previewJoinStatus) previewJoinStatus.textContent = 'Please enter a valid room code.';
                return;
            }
            hideAllHomeModals();
            navigateToScreen('waiting');
        });
    }
    if (friendModalCloseBtn) {
        friendModalCloseBtn.addEventListener('click', hideAllHomeModals);
    }

    if (gameMenuBtn) gameMenuBtn.addEventListener('click', () => navigateToScreen('home'));

    if (leaveButton && leaveConfirmModal) {
        leaveButton.addEventListener('click', () => leaveConfirmModal.classList.remove('hidden'));
    }
    if (leaveModalConfirmBtn && leaveConfirmModal) {
        leaveModalConfirmBtn.addEventListener('click', () => {
            leaveConfirmModal.classList.add('hidden');
            navigateToScreen('home');
        });
    }
    if (leaveModalCancelBtn && leaveConfirmModal) {
        leaveModalCancelBtn.addEventListener('click', () => leaveConfirmModal.classList.add('hidden'));
    }
    if (alertCloseXBtn && leaveConfirmModal) {
        alertCloseXBtn.addEventListener('click', () => leaveConfirmModal.classList.add('hidden'));
    }

    if (homeProfileBtn && profileStatsModal) {
        homeProfileBtn.addEventListener('click', () => {
            hideAllHomeModals();
            profileStatsModal.classList.remove('hidden');
        });
    }
    if (profileCloseBtn && profileStatsModal) {
        profileCloseBtn.addEventListener('click', hideAllHomeModals);
    }
    if (homeHamburgerBtn && homeSettingsModal) {
        homeHamburgerBtn.addEventListener('click', () => {
            hideAllHomeModals();
            homeSettingsModal.classList.remove('hidden');
        });
    }
    if (homeSettingsCloseBtn && homeSettingsModal) {
        homeSettingsCloseBtn.addEventListener('click', hideAllHomeModals);
    }
});
