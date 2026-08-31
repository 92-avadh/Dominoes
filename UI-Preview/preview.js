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
    const lobbyBackBtn = document.getElementById('lobby-back-btn');
    const startGameBtn = document.getElementById('start-game-btn');
    const gameMenuBtn = document.getElementById('game-menu-btn');

    if (modeOnlineBtn) modeOnlineBtn.addEventListener('click', () => navigateToScreen('lobby'));
    if (modeComputerBtn) modeComputerBtn.addEventListener('click', () => navigateToScreen('lobby'));
    if (modeFriendsBtn) modeFriendsBtn.addEventListener('click', () => navigateToScreen('lobby'));
    if (lobbyBackBtn) lobbyBackBtn.addEventListener('click', () => navigateToScreen('home'));
    if (startGameBtn) startGameBtn.addEventListener('click', () => navigateToScreen('waiting'));
    if (gameMenuBtn) gameMenuBtn.addEventListener('click', () => navigateToScreen('home'));
});
