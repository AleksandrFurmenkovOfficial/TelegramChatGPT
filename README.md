# ChatGPT Telegram Bot 
[![.NET](https://github.com/AleksandrFurmenkovOfficial/TelegramChatGPT/actions/workflows/dotnet.yml/badge.svg)](https://github.com/AleksandrFurmenkovOfficial/TelegramChatGPT/actions/workflows/dotnet.yml)

## Overview
This repository contains the source code for a free telegram bot that integrates with ChatGPT. The bot is designed to provide user a variety of functionalities including image recognition, image generation, internet search for answering simple queries, and a note-taking feature to record notes between dialogues.

## Features
- **Image Recognition:** The bot can analyze images sent by users and provide context or information about the content of the image.
- **Image Generation:** Users can request the bot to generate images based on specific prompts or descriptions.
- **Internet Search:** The bot can search the internet to find answers to simple questions posed by users.
- **Note-Taking:** The bot includes a personal diary feature where users can store notes, reminders, or any text for future reference between dialogues.
- **Different modes:** The bot can switch between different modes: general mode, teacher mode, etc.

## Getting Started

**From sources:**
1. Clone the Repository
2. Install the .NET 8 SDK & runtime for your operating system from https://dotnet.microsoft.com/en-us/download/dotnet/8.0
3. Build the application
4. Set up these variables:
```
OPENAI_API_KEY: For AI functionalities.
TELEGRAM_BOT_KEY: To connect your bot.
TELEGRAM_ADMIN_USER_ID: (Optional) For administrative privileges.
```
6. Run the application, chat via your bot with ChatGPT

**From binaries:**
1. Download binaries from latest release
2. Set up these variables:
```
OPENAI_API_KEY: For AI functionalities.
TELEGRAM_BOT_KEY: To connect your bot.
TELEGRAM_ADMIN_USER_ID: (Optional) For administrative privileges.
```
3. Run the application, chat via your bot with ChatGPT
