# Enjin NFT Sample Game Server

This is a NodeJS Express RESTful server for a sample Unity game [Enjin Farmer](https://github.com/enjin/platform-sample-game-client-unity), designed to demonstrate how to easily integrate NFTs into a project using the Enjin Platform.

## Overview

This server provides the backend functionality for managing NFTs within the sample Unity game. It handles user authentication, wallet management, and token operations (minting, melting, and transferring) using the Enjin Platform API.

## Features

*   User registration and login
*   Managed wallet creation and retrieval
*   Token minting, melting, and transfer
*   Health check endpoint

## Prerequisites

*   Node.js and npm installed
*   An Enjin Platform account and API key
*   (Optional) Wallet Daemon

## Setup Instructions

1.  **Clone the repository:**

    ```bash
    git clone https://github.com/enjin/platform-sample-game-server.git
    cd platform-sample-game-server
    ```

2.  **Install dependencies:**

    ```bash
    npm install
    ```

3.  **Configure environment variables:**

    Duplicate the `.env.example` file and rename the copy to `.env`.
    Open the `.env` file and fill in the following variables:
    - `PORT=3000` (You can change this if port 3000 is already in use).
    - `APP_KEY`: Generate a secure, random string. This key ensures that only your game client can communicate with your server.
    - `JWT_SECRET`: Generate another secure, random string. This is used for authenticating players.
    - `ENJIN_API_URL`: Keep the default `https://platform.canary.enjin.io/graphql` for testing on the Canary network.
    - `ENJIN_API_KEY`: Paste the **API Key Token** from your Enjin Platform account.
    - `DAEMON_WALLET_ADDRESS`: Paste the wallet address you copied from the Wallet Daemon UI.
    - `ENJIN_COLLECTION_ID`: Leave this blank, it will be automatically populated once the collection is created.

4.  **Run the server:**

    ```bash
    npm start
    ```

    On the first server launch, a collection will be created, along with the resources tokens.
    Once these are created, the server will run on port 3000.

## API Endpoints

- `/api/auth/health-check`: Perform a health check to ensure the server is running and authentication is working.
- `/api/auth/register`: User registration
- `/api/auth/login`: User login
- `/api/wallet/create`: Create a managed wallet
- `/api/wallet/get`: Get a managed wallet
- `/api/wallet/get-tokens`: Get a managed wallet and its tokens
- `/api/token/mint`: Mint token
- `/api/token/melt`: Melt token
- `/api/token/transfer`: Transfer token

## Enjin Platform Documentation

For more in-depth information about the Enjin Platform and its features, please refer to the official documentation:

[Placeholder: Link to Enjin Documentation]