import os
import wave
import struct
import math
import random

audio_dir = r"c:\Users\dhame\Dominoes_MVP_Prototype\Assets\Audio"
os.makedirs(audio_dir, exist_ok=True)
SAMPLE_RATE = 44100

def write_wav(filename, samples):
    filepath = os.path.join(audio_dir, filename)
    with wave.open(filepath, "w") as wav_file:
        wav_file.setnchannels(1)        # Mono
        wav_file.setsampwidth(2)        # 16-bit
        wav_file.setframerate(SAMPLE_RATE)
        
        packed_samples = bytearray()
        for sample in samples:
            # Clamp between -1.0 and 1.0
            clamped = max(-1.0, min(1.0, sample))
            int_val = int(clamped * 32767.0)
            packed_samples.extend(struct.pack("<h", int_val))
            
        wav_file.writeframes(packed_samples)
    print(f"Generated {filename} ({len(samples)/SAMPLE_RATE:.2f}s)")

# 1. UI Button Click
def generate_sfx_click():
    duration = 0.035
    num_samples = int(SAMPLE_RATE * duration)
    samples = []
    for i in range(num_samples):
        t = i / SAMPLE_RATE
        env = math.exp(-t * 120.0)
        freq = 1400.0 - t * 25000.0
        sample = math.sin(2.0 * math.pi * max(100.0, freq) * t) * env * 0.45
        samples.append(sample)
    write_wav("sfx_click.wav", samples)

# 2. Tile Pickup / Touch
def generate_sfx_tile_pickup():
    duration = 0.065
    num_samples = int(SAMPLE_RATE * duration)
    samples = []
    for i in range(num_samples):
        t = i / SAMPLE_RATE
        env = math.exp(-t * 60.0)
        s1 = math.sin(2.0 * math.pi * 750.0 * t) * 0.5
        s2 = math.sin(2.0 * math.pi * 1500.0 * t) * 0.3
        s3 = (random.random() * 2.0 - 1.0) * 0.15 * math.exp(-t * 180.0)
        sample = (s1 + s2 + s3) * env * 0.4
        samples.append(sample)
    write_wav("sfx_tile_pickup.wav", samples)

# 3. Domino Placement Clack (Crisp physical weighted impact)
def generate_sfx_tile_clack():
    duration = 0.12
    num_samples = int(SAMPLE_RATE * duration)
    samples = []
    for i in range(num_samples):
        t = i / SAMPLE_RATE
        env_body = math.exp(-t * 48.0)
        env_transient = math.exp(-t * 280.0)
        
        # Transient crack
        noise = (random.random() * 2.0 - 1.0) * env_transient * 0.75
        # Ceramic / Ivory body resonant frequencies
        f1 = math.sin(2.0 * math.pi * 840.0 * t) * 0.55
        f2 = math.sin(2.0 * math.pi * 1680.0 * t) * 0.35
        f3 = math.sin(2.0 * math.pi * 2520.0 * t) * 0.2
        f4 = math.sin(2.0 * math.pi * 420.0 * t) * 0.3 * math.exp(-t * 70.0)
        
        sample = (noise + (f1 + f2 + f3 + f4) * env_body) * 0.7
        samples.append(sample)
    write_wav("sfx_tile_clack.wav", samples)

# 4. Draw Tile Shuffle
def generate_sfx_tile_draw():
    duration = 0.16
    num_samples = int(SAMPLE_RATE * duration)
    samples = []
    for i in range(num_samples):
        t = i / SAMPLE_RATE
        env = math.sin(math.pi * (t / duration)) ** 1.5
        noise = (random.random() * 2.0 - 1.0) * 0.4
        tone = math.sin(2.0 * math.pi * 620.0 * t + math.sin(2.0 * math.pi * 14.0 * t) * 2.0) * 0.25
        sample = (noise + tone) * env * 0.5
        samples.append(sample)
    write_wav("sfx_tile_draw.wav", samples)

# 5. Turn Notification Chime (2-note ascending)
def generate_sfx_turn_chime():
    duration = 0.42
    num_samples = int(SAMPLE_RATE * duration)
    samples = []
    for i in range(num_samples):
        t = i / SAMPLE_RATE
        sample = 0.0
        
        # Note 1: E5 (659Hz) at t=0
        if t < 0.35:
            env1 = math.exp(-t * 14.0)
            sample += math.sin(2.0 * math.pi * 659.25 * t) * env1 * 0.35
            sample += math.sin(2.0 * math.pi * 1318.5 * t) * env1 * 0.12
            
        # Note 2: A5 (880Hz) at t=0.12
        if t >= 0.11:
            t2 = t - 0.11
            env2 = math.exp(-t2 * 10.0)
            sample += math.sin(2.0 * math.pi * 880.0 * t2) * env2 * 0.45
            sample += math.sin(2.0 * math.pi * 1760.0 * t2) * env2 * 0.15
            
        samples.append(sample * 0.6)
    write_wav("sfx_turn_chime.wav", samples)

# 6. Pass Turn Sound
def generate_sfx_pass():
    duration = 0.22
    num_samples = int(SAMPLE_RATE * duration)
    samples = []
    for i in range(num_samples):
        t = i / SAMPLE_RATE
        env = math.exp(-t * 16.0)
        freq = 460.0 - t * 600.0
        sample = math.sin(2.0 * math.pi * max(120.0, freq) * t) * env * 0.35
        samples.append(sample)
    write_wav("sfx_pass.wav", samples)

# 7. Win Fanfare
def generate_sfx_win():
    duration = 0.95
    num_samples = int(SAMPLE_RATE * duration)
    samples = []
    # C5, E5, G5, C6 arpeggio
    notes = [
        (0.00, 523.25, 0.4),
        (0.12, 659.25, 0.45),
        (0.24, 783.99, 0.5),
        (0.36, 1046.50, 0.65)
    ]
    for i in range(num_samples):
        t = i / SAMPLE_RATE
        sample = 0.0
        for (start_t, freq, amp) in notes:
            if t >= start_t:
                dt = t - start_t
                env = math.exp(-dt * 6.5)
                s = math.sin(2.0 * math.pi * freq * dt)
                h = math.sin(2.0 * math.pi * freq * 2.0 * dt) * 0.25
                sample += (s + h) * env * amp
        samples.append(sample * 0.4)
    write_wav("sfx_win.wav", samples)

# 8. Loss / Mellow Finish
def generate_sfx_loss():
    duration = 0.55
    num_samples = int(SAMPLE_RATE * duration)
    samples = []
    notes = [
        (0.00, 440.0, 0.4),
        (0.18, 370.0, 0.35)
    ]
    for i in range(num_samples):
        t = i / SAMPLE_RATE
        sample = 0.0
        for (start_t, freq, amp) in notes:
            if t >= start_t:
                dt = t - start_t
                env = math.exp(-dt * 8.0)
                sample += math.sin(2.0 * math.pi * freq * dt) * env * amp
        samples.append(sample * 0.45)
    write_wav("sfx_loss.wav", samples)

# 9. Ambient Relaxing Casual BGM Loop (8.0 seconds)
def generate_music_ambient_loop():
    duration = 8.0
    num_samples = int(SAMPLE_RATE * duration)
    samples = [0.0] * num_samples
    
    # 4 gentle chords over 8s: Cmaj7 (0-2s), Fmaj7 (2-4s), Am7 (4-6s), Gsus4 (6-8s)
    chords = [
        (0.0, [261.63, 329.63, 392.00, 493.88]), # Cmaj7 (C4, E4, G4, B4)
        (2.0, [349.23, 440.00, 523.25, 659.25]), # Fmaj7 (F4, A4, C5, E5)
        (4.0, [220.00, 261.63, 329.63, 392.00]), # Am7 (A3, C4, E4, G4)
        (6.0, [196.00, 261.63, 293.66, 392.00])  # Gsus4 (G3, C4, D4, G4)
    ]
    
    for (start_time, freqs) in chords:
        for f in freqs:
            for i in range(num_samples):
                t = i / SAMPLE_RATE
                if t >= start_time and t < start_time + 2.0:
                    dt = t - start_time
                    # Gentle attack and release envelope
                    env = math.sin(math.pi * (dt / 2.0))
                    val = math.sin(2.0 * math.pi * f * dt) * 0.08 * env
                    samples[i] += val

    # Seamless loop boundary smoothing
    for i in range(int(SAMPLE_RATE * 0.05)):
        ratio = i / (SAMPLE_RATE * 0.05)
        samples[i] *= ratio
        samples[num_samples - 1 - i] *= ratio

    write_wav("music_ambient_loop.wav", samples)

if __name__ == "__main__":
    generate_sfx_click()
    generate_sfx_tile_pickup()
    generate_sfx_tile_clack()
    generate_sfx_tile_draw()
    generate_sfx_turn_chime()
    generate_sfx_pass()
    generate_sfx_win()
    generate_sfx_loss()
    generate_music_ambient_loop()
