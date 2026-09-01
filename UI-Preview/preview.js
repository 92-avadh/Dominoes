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
                // Apply hand zoom when switching to game screen
                if (screenId === 'game') {
                    setTimeout(applyHandZoom, 50);
                }
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

    // 2.5. Background Image Switcher (BG 1 - BG 9)
    const bgButtons = document.querySelectorAll('#bg-switcher .bg-btn');

    bgButtons.forEach(btn => {
        btn.addEventListener('click', () => {
            bgButtons.forEach(b => b.classList.remove('active'));
            btn.classList.add('active');

            const bgNum = btn.getAttribute('data-bg');
            if (bgNum) {
                const activeScreen = document.querySelector('.ui-screen.active');
                if (activeScreen) {
                    const rootElem = activeScreen.querySelector('.home-root, .loading-root, .waiting-root, .table-surface, .game-root');
                    if (rootElem) {
                        rootElem.style.backgroundImage = `linear-gradient(180deg, rgba(15, 23, 42, 0.45) 0%, rgba(6, 78, 59, 0.7) 100%), url('../Assets/images/bg_${bgNum}.jpg')`;
                        rootElem.style.backgroundSize = 'cover';
                        rootElem.style.backgroundPosition = 'center';
                    }
                }
            }
        });
    });

    // 2.6. Hand Tile Responsive Zoom
    // Dynamically applies --compact (8-10 tiles) or --mini (11+) class for smooth zoom animation
    function applyHandZoom() {
        const gameScreen = document.getElementById('screen-game');
        if (!gameScreen) return;

        const handContainer = gameScreen.querySelector('.hand-tiles-container');
        if (!handContainer) return;

        const tiles = handContainer.querySelectorAll('.domino-tile-hand');
        const count = tiles.length;

        tiles.forEach(tile => {
            tile.classList.remove('domino-tile-hand--compact', 'domino-tile-hand--mini');
            if (count >= 11) {
                tile.classList.add('domino-tile-hand--mini');
            } else if (count >= 8) {
                tile.classList.add('domino-tile-hand--compact');
            }
        });

        // Update tile count label
        const countLabel = gameScreen.querySelector('.hand-header-count');
        if (countLabel) {
            countLabel.textContent = `${count} TILES`;
        }
    }

    // Observe hand tile container for dynamic tile count changes
    const gameScreenEl = document.getElementById('screen-game');
    if (gameScreenEl) {
        const handContainer = gameScreenEl.querySelector('.hand-tiles-container');
        if (handContainer) {
            const observer = new MutationObserver(() => applyHandZoom());
            observer.observe(handContainer, { childList: true });
        }
        applyHandZoom();
    }

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
