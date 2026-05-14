# VibeCheck — Hackathon Project Summary

> Real-Time Classroom Quiz Platform  
> Built with .NET 10 · Blazor Server · SignalR

---

## Problem

ห้องเรียนและ training session ขาด tool ที่ทำให้ผู้เรียน **engage** ได้ทันที  
Kahoot มีฟีเจอร์ครบ แต่ต้องสมัครสมาชิก, จ่ายเงิน, และผูกกับ cloud ของบริษัทอื่น  
**VibeCheck** คือ open-source alternative ที่ deploy เองได้, ไม่มี account, ใช้งานได้ใน 30 วินาที

---

## Solution

Host สร้าง Quiz → แชร์ PIN 6 หลัก (หรือ QR Code) → ผู้เรียนเข้าจากมือถือ → เล่นได้เลย

```
Host /host  ──→  PIN: 482 916  ──→  Players /play
                 QR Code ↗
```

---

## Tech Stack

| Layer | Technology | เหตุผลที่เลือก |
|-------|-----------|----------------|
| Backend + Frontend | .NET 10 Blazor Server | Single project — ไม่ต้องแยก API, C# ทั้งหมด |
| Real-time | ASP.NET Core SignalR | WebSocket push — timer/score sync ทุก client พร้อมกัน |
| Styling | Tailwind CSS + Material Symbols | ไม่ต้อง build step, CDN ใช้งานได้ทันที |
| QR Code | QRCoder (NuGet) | Inline SVG — ไม่ต้องเรียก API ภายนอก |
| Audio | Web Audio API (JS) | ไม่ต้องโหลด sound library |
| Confetti | canvas-confetti (CDN) | 3KB, ไม่มี dependency |
| Hosting | Render.com (Docker) | Free tier, WebSocket support |

---

## Architecture

```
Browser (Host)          Server                    Browser (Player)
     │                    │                              │
     │── HostCreateSession ──▶ GameHub (SignalR) ──────▶ │
     │◀── SessionCreated ──── GameService ◀────────────── │── JoinGame
     │                    │   TimerService               │
     │── StartGame ────────▶ (timer loop) ──── QuestionStarted ──▶ │
     │◀── TimerTick ──────── TimerTick ──────────────────▶ │
     │◀── QuestionEnded ───── EndQuestion ───────────────▶ │
     │── NextStep ─────────▶ BroadcastLeaderboard ──────▶ │
```

**Server-authoritative:** Timer วิ่งบน server → ทุก client เห็นเวลาเดียวกัน, ป้องกันโกง  
**State machine:** `Lobby → ShowQuestion → ShowAnswers → Leaderboard → Finished`  
**Thread-safe:** `ConcurrentDictionary` ทุกที่, `lock(session)` ป้องกัน race condition บน EndQuestion

---

## Features Built

### V.1 — Core Game Loop (Phase 0–6)

| Feature | รายละเอียด |
|---------|-----------|
| Quiz Management | สร้าง / แก้ไข / ลบ quiz, ≤50 ข้อ, 2–4 ตัวเลือกต่อข้อ |
| Host Lobby | PIN ใหญ่, QR Code, รายชื่อ player real-time |
| Player Join | PIN + Nickname, validate ซ้ำ, reject หลัง game start |
| Question Flow | 4 ปุ่มสี (Red/Blue/Yellow/Green), lock หลังกด |
| Server Timer | Countdown per question (5–60 วิ), broadcast ทุก 1 วิ |
| Scoring | Speed-based: ตอบเร็ว = คะแนนเยอะ (50–100% of base) |
| Answer Count | Host เห็น X/Y ตอบแล้ว real-time |
| Leaderboard | Top 5 + rank ของตัวเอง หลังทุกข้อ |
| Quiz Import/Export | CSV import สร้าง quiz จำนวนมาก, CSV export ผลคะแนน |

### V.2 — Polish & Resilience

| Feature | รายละเอียด |
|---------|-----------|
| CountdownRing | SVG ring animation, สีเปลี่ยน: green → yellow → red |
| Sound Effects | Web Audio API: ถูก/ผิด/tick/start/finish (ไม่ใช้ไฟล์ภายนอก) |
| Background Music | เพลงระหว่างเกม, mute toggle |
| Confetti | canvas-confetti เมื่อ rank ≤3 |
| Answer Distribution | Host เห็น bar chart per option หลังข้อจบ |
| Host Reconnect | Host หลุดได้, game ดำเนิน auto, host rejoin ได้ทันที |
| Session Cleanup | Background service กวาด session เก่าทุก 10 นาที |
| HostDisconnected UX | Player เห็น banner แจ้ง, dismiss เมื่อ host กลับมา |

---

## Game Flow (สำหรับ Demo)

```
1. เปิด /host → เลือก "Demo Quiz" → ได้ PIN เช่น 482916
2. เพื่อน scan QR หรือเปิด /play → ใส่ PIN + ชื่อ
3. กด "Start Game" → ข้อแรกขึ้น
4. ทุกคนเห็น countdown ring เดียวกัน
5. ตอบก่อน = คะแนนเยอะกว่า
6. Host เห็น answer distribution หลังข้อจบ
7. Leaderboard หลังทุกข้อ
8. จบ: confetti + CSV export ผลคะแนน
```

---

## What We Built in the Hackathon

| วัน | ทำอะไร |
|-----|--------|
| Day 1 | Scaffold, Models, State Machine, GameService, TimerService |
| Day 2 | GameHub (SignalR), Host UI, Player UI |
| Day 3 | Quiz Builder, CRUD, Edge cases, QR Code |
| Day 4 (V.2) | Host reconnect, Session cleanup, Answer distribution, CountdownRing |
| Day 5 (V.2) | Sound effects, Confetti, Mute toggle, Background music, CSV import/export |

**Total:** ~8 commits, ~2,500 lines of C#/Razor/JS

---

## Live Demo

🌐 **https://vibecheck-production-ca62.up.railway.app**

- Host: `/host`
- Join: `/play`
- Quiz ตัวอย่างพร้อมเล่นทันที (Demo Quiz 3 ข้อ)

---

## GitHub

📦 **https://github.com/weeraphatcs-prog/VibeCheck**

---

---

## Changelog

### V.1 — Core Game Loop
- Scaffold, Models, State Machine (`Lobby → ShowQuestion → ShowAnswers → Leaderboard → Finished`)
- `GameService` (singleton, thread-safe `ConcurrentDictionary`)
- `TimerService` (server-authoritative countdown, broadcast ทุก 1 วิ)
- `GameHub` SignalR: `HostCreateSession`, `JoinGame`, `SubmitAnswer`, `NextStep`, `OnDisconnectedAsync`
- Host UI + Player UI (Blazor Server, Tailwind CSS dark theme)
- Quiz CRUD (`/quizzes` index, create, edit, delete)
- QR Code generation (QRCoder, inline SVG)
- Speed-based scoring algorithm: `Points × (0.5 + 0.5 × speedRatio)`
- PIN validation: 6-digit, unique, server-generated

### V.2 — Polish & Resilience
- **Host reconnect**: `HostJoin` hub method — host หลุดได้, game ดำเนิน auto, rejoin ได้ทันที
- **SessionCleanupService**: Background service กวาด session เก่า (inactive > 2h) ทุก 10 นาที
- **Answer distribution**: Host เห็น count per option หลังข้อจบ (ใน `QuestionEnded` event)
- **CountdownRing**: SVG ring animation, สีเปลี่ยน green→yellow→red ตาม threshold (>50%, 25–50%, <25%)
- **Web Audio effects**: Web Audio API synthesized — correct/wrong/tick/start/finish (ไม่มีไฟล์ภายนอก)
- **Background music**: `<audio>` element loop, mute toggle sync กับ sound effects
- **Confetti**: canvas-confetti CDN, เฉพาะ player ที่ rank ≤3
- **CSV import**: bulk create quiz จาก `.csv` (maxAllowedSize 1MB, parse manual ไม่ใช้ library)
- **CSV export**: download leaderboard ผลคะแนนหลัง game finish

### V.2 Security Patch (May 14, 2026)
- **Fixed CWE-20** (`Create.razor:161`): `TimeLimitSec` จาก CSV import ไม่ได้ validate — แก้โดย allowlist `[5, 10, 15, 20, 30, 60]` เท่านั้น; ค่านอก list fallback เป็น 20
- **Fixed CWE-209** (`Create.razor:168`): raw `ex.Message` แสดงใน UI — แก้เป็น generic error message

**ไม่พบ** (confirmed clean): SQL injection (ไม่มี DB), XSS (Blazor escape auto), hardcoded secrets, plaintext passwords

---

## Known Limitations → V.3 Backlog

| จุด | ผลกระทบ | แนวทางแก้ |
|-----|---------|-----------|
| In-memory storage | ข้อมูลหายเมื่อ restart | SQLite + EF Core |
| No rate limiting บน `JoinGame` | PIN brute-force ได้ใน theory | SignalR rate limit middleware / Redis |
| No auth บน Quiz CRUD | ใครก็ลบ quiz ได้ | Admin PIN หรือ simple JWT |
| `AllowedHosts: "*"` | Host header injection risk | Lock down ใน production `appsettings` |
| `Random.Shared` สำหรับ PIN | ไม่ใช่ crypto-random | `RandomNumberGenerator.GetInt32()` |
| No HTTPS redirect ใน code | ขึ้นกับ platform (Render) | `app.UseHttpsRedirection()` |
| Unlimited quiz creation | Memory DoS ได้ | Max quiz count per instance |
| Image/media ใน question | ข้อสอบมีแค่ text | Blob storage + `<img>` support |
| Team mode | เล่นเดี่ยวอย่างเดียว | Group model ใน `GameSession` |
| Mobile PWA | ต้องเปิด browser | Service Worker + manifest |
