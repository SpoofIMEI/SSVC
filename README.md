### <img width=800 src="https://github.com/user-attachments/assets/c199a355-a3a8-4e58-b3ef-f264a64eb18f"></img>

[![C#](https://img.shields.io/badge/c%23-%23239120.svg?style=for-the-badge&logo=csharp&logoColor=white)](https://img.shields.io/badge/c%23-%23239120.svg?style=for-the-badge&logo=csharp&logoColor=white)
[![.NET](https://img.shields.io/badge/.NET-5C2D91?style=for-the-badge&logo=.net&logoColor=white)](https://img.shields.io/badge/.NET-5C2D91?style=for-the-badge&logo=.net&logoColor=white)

<b>SSVC (Somewhat Secure Voice Changer) is a voice changer that is much harder to reverse and indetify people behind than traditional voice changers. It does this by randomly alternating the pitch and/or different frequencies in the sound.</b>

## Why?
If you ever need to talk to someone that you suspect might try figure out information about you for malicious purposes, you would probably want to alter your voice so that it cannot be used to identify you. Traditional voice changers typically do the job, but with the advancements of AI, I was able to correlate the pitch shifted version of a person's voice so it was clear that a normal voice changer would be insufficient to conceal your identity if the attacker had another recording of you **WITHOUT** the voice changer. 

## How?
To combat this, I combined constantly shifting the pitch and randomly making certain frequencies louder or quiter, which worked on the AI flawlessly. You had to turn down the distortion factor a significant amount before it could detect that it was the same person again.

## Demo
### With SSVC
**Settings:**
```
Pitch: 15
Distortion: 10
```
<img width=500 src="https://github.com/user-attachments/assets/250166d3-492e-49ce-82d6-2fbf2c31fd5f"></img>

### Without SSVC
<img width=500 src="https://github.com/user-attachments/assets/a03f6380-301e-46ce-885b-6effb4c03739"></img>
