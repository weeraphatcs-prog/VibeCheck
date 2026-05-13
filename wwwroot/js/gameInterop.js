let _audioCtx = null;
let _muted = false;

function ctx() {
    if (!_audioCtx) _audioCtx = new (window.AudioContext || window.webkitAudioContext)();
    if (_audioCtx.state === 'suspended') _audioCtx.resume();
    return _audioCtx;
}

function tone(freq, duration, type = 'sine', vol = 0.3) {
    if (_muted) return;
    try {
        const c = ctx();
        const osc = c.createOscillator();
        const gain = c.createGain();
        osc.connect(gain);
        gain.connect(c.destination);
        osc.type = type;
        osc.frequency.value = freq;
        gain.gain.setValueAtTime(vol, c.currentTime);
        gain.gain.exponentialRampToValueAtTime(0.001, c.currentTime + duration);
        osc.start(c.currentTime);
        osc.stop(c.currentTime + duration);
    } catch (e) { }
}

window.gameInterop = {
    setMuted: (val) => { _muted = val; },
    isMuted: () => _muted,

    playCorrect: () => {
        tone(523, 0.1);
        setTimeout(() => tone(659, 0.1), 100);
        setTimeout(() => tone(784, 0.2), 200);
    },

    playWrong: () => {
        tone(220, 0.15, 'sawtooth', 0.15);
        setTimeout(() => tone(180, 0.25, 'sawtooth', 0.1), 150);
    },

    playTimerTick: () => { tone(880, 0.05, 'square', 0.08); },

    playGameStart: () => {
        [523, 659, 784, 1047].forEach((f, i) =>
            setTimeout(() => tone(f, 0.15), i * 120));
    },

    playQuestionStart: () => {
        tone(659, 0.1, 'triangle', 0.2);
        setTimeout(() => tone(784, 0.15, 'triangle', 0.2), 120);
    },

    playFinish: () => {
        [523, 659, 784, 1047, 1319].forEach((f, i) =>
            setTimeout(() => tone(f, 0.25), i * 100));
    },

    launchConfetti: () => {
        if (typeof confetti === 'undefined') return;
        confetti({ particleCount: 150, spread: 70, origin: { y: 0.6 } });
        setTimeout(() => confetti({ particleCount: 80, angle: 60, spread: 55, origin: { x: 0 } }), 400);
        setTimeout(() => confetti({ particleCount: 80, angle: 120, spread: 55, origin: { x: 1 } }), 400);
    }
};
