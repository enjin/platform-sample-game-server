const axios = require('axios');
const fs = require('fs');
const Pusher = require('pusher-js/node');

// Resource token definitions
const RESOURCE_TOKENS = [
    { id: 1, name: "Gold Coin", media: "[{\"type\":\"image/png\",\"url\":\"https://cdn.enjin.io/mint/image/gold-coin.png\"}]"},
    { id: 2, name: "Gold Coin (Blue)", media: "[{\"type\":\"image/png\",\"url\":\"https://cdn.enjin.io/mint/image/gold-coin-blue.png\"}]" },
    { id: 3, name: "Green Gem", media: "[{\"type\":\"image/png\",\"url\":\"https://cdn.enjin.io/mint/image/green-gem.png\"}]" }
];

async function sleep(ms) {
    return new Promise(resolve => setTimeout(resolve, ms));
}

async function extractCollectionId(transaction) {
    if (!transaction.events || !transaction.events.edges) {
        return null;
    }

    for (const edge of transaction.events.edges) {
        const params = edge.node.params;
        const collectionIdParam = params.find(p => p.type === 'collection_id');
        if (collectionIdParam) {
            return collectionIdParam.value;
        }
    }

    return null;
}

async function updateEnvFile(collectionId) {
    const envContent = fs.readFileSync('.env', 'utf8');
    const updatedContent = envContent.replace(
        /ENJIN_COLLECTION_ID=.*/,
        `ENJIN_COLLECTION_ID=${collectionId}`
    );
    fs.writeFileSync('.env', updatedContent);
    process.env.ENJIN_COLLECTION_ID = collectionId;
}

async function createCollection(){
    const response = await axios.post(process.env.ENJIN_API_URL, {
        query: `mutation CreateCollection(
    $forceCollapsingSupply: Boolean
    $name: String!
    $bannerImage: String!
    $media: String!
) {
    CreateCollection(
        mintPolicy: { forceCollapsingSupply: $forceCollapsingSupply }
        attributes:[
            {
                key: \"name\",
                value: $name
            },
            {
                key: \"banner_image\",
                value: $bannerImage
            },
            {
                key: \"media\",
                value: $media
            }
        ]
    ) {
        id
        method
        state
    }
}`,
        variables: {
            forceCollapsingSupply: false,
            name: "Enjin Sample Game",
            bannerImage: "https://cdn.enjin.io/mint/image/sample-game-collection-banner.png",
            media: "[{\"type\":\"image/png\",\"url\":\"https://cdn.enjin.io/mint/image/sample-game-collection-image.png\"}]"

        }
    }, {
        headers: {
            'Content-Type': 'application/json',
            'Authorization': process.env.ENJIN_API_KEY
        }
    });

    return response;
}

async function createToken(collectionId, tokenId, name, media){
    const response = await axios.post(process.env.ENJIN_API_URL, {
                query: `mutation CreateToken(
    $recipient: String!
    $collectionId: BigInt!
    $tokenId: BigInt
    $name: String!
    $media: String!
){
  CreateToken(
    recipient: $recipient
    collectionId: $collectionId
    params:{
      tokenId: {integer: $tokenId}
      initialSupply: 1
      attributes: [
      {
      	key: "name",
      	value: $name
    	},
      {
      	key: "media",
      	value: $media
    	}
    ]
    }
  ){
    id
    method
    state
  }
}`,
        variables: {
            recipient: process.env.DAEMON_WALLET_ADDRESS || "5EJDmqEoySnLk8xvPNPQGrb9qUrYLcbf38K4R6zKeNryvfD6",
            collectionId: collectionId,
            tokenId: tokenId,
            name: name,
            media: media
        }
    }, {
        headers: {
            'Content-Type': 'application/json',
            'Authorization': process.env.ENJIN_API_KEY
        }
    });

    return response;
}

async function getToken(collectionId, tokenId) {
    const response = await axios.post(process.env.ENJIN_API_URL, {
        query: `query GetToken{
  GetToken(
    collectionId: ${collectionId}
    tokenId: {integer: ${tokenId}}
  ){
    metadata
  }
}`,
    }, {
        headers: {
            'Content-Type': 'application/json',
            'Authorization': process.env.ENJIN_API_KEY
        }
    });

    return response;
}

async function getWalletPublicKey(address){
    const response = await axios.post(process.env.ENJIN_API_URL, {
        query: `query getWalletPublicKey($address: String){
  GetWallet(account: $address){
    account{
      publicKey
      address
    }
  }
}`,
        variables: {
            address: address
        }
    }, {
        headers: {
            'Content-Type': 'application/json',
            'Authorization': process.env.ENJIN_API_KEY
        }
    });

    return response.data.data.GetWallet;
}

// ------------------------
// WebSocket (Pusher) setup
// ------------------------

let enjinPusher = null;
let enjinChannel = null;
let daemonPublicKeyCache = null;
let wsInitPromise = null;

// Track waiters by transaction id
const transactionWaiters = new Map(); // id -> { resolve, reject, operationType }

function parsePusherConfigFromUrl(urlStr) {
    try {
        const u = new URL(urlStr);
        const path = u.pathname || '';
        const appKey = path.includes('/app/') ? path.split('/app/')[1].split('?')[0] : '';
        // host pattern like ws-us2.pusher.com
        const host = u.hostname || '';
        const sub = host.split('.')[0] || '';
        const cluster = sub.startsWith('ws-') ? sub.substring(3) : sub; // remove 'ws-'
        return { appKey, cluster };
    } catch (e) {
        return { appKey: '', cluster: '' };
    }
}

async function ensureWebsocketInitialized() {
    if (wsInitPromise) return wsInitPromise;

    wsInitPromise = (async () => {
        if (!process.env.ENJIN_PLATFORM_WEBSOCKET_URL) {
            throw new Error('ENJIN_PLATFORM_WEBSOCKET_URL is not set');
        }
        if (!process.env.DAEMON_WALLET_ADDRESS) {
            throw new Error('DAEMON_WALLET_ADDRESS is not set');
        }

        // Get daemon wallet public key once and cache it
        if (!daemonPublicKeyCache) {
            const wallet = await getWalletPublicKey(process.env.DAEMON_WALLET_ADDRESS);
            if (!wallet || !wallet.account || !wallet.account.publicKey) {
                throw new Error('Failed to resolve daemon wallet public key');
            }
            daemonPublicKeyCache = wallet.account.publicKey;
        }

        const { appKey, cluster } = parsePusherConfigFromUrl(process.env.ENJIN_PLATFORM_WEBSOCKET_URL);
        if (!appKey) {
            throw new Error('Invalid ENJIN_PLATFORM_WEBSOCKET_URL: cannot extract app key');
        }

        // Initialize Pusher client
        enjinPusher = new Pusher(appKey, {
            cluster: cluster || undefined,
            forceTLS: true,
        });

        // Subscribe to daemon wallet public key channel
        enjinChannel = enjinPusher.subscribe(daemonPublicKeyCache);

        // Handle events relevant to transactions
        const handleEvent = async (eventName, payload) => {
            try {
                const data = typeof payload === 'string' ? JSON.parse(payload) : payload;
                if (!data || typeof data.id !== 'number') return;
                const waiter = transactionWaiters.get(data.id);
                if (!waiter) return; // Not waiting on this tx

                // If there's no definitive state yet, ignore
                if (
                    data.state === 'PENDING' ||
                    data.state === 'PROCESSING' ||
                    data.state === 'BROADCAST' ||
                    data.state === 'EXECUTED'
                ) return;

                if (data.state === 'FINALIZED' && data.result === 'EXTRINSIC_SUCCESS') {
                    transactionWaiters.delete(data.id);
                    waiter.resolve(data);
                } else if (
                    data.state === 'FAILED' ||
                    data.state === 'ABANDONED' ||
                    data.result === 'EXTRINSIC_FAILED'
                ) {
                    transactionWaiters.delete(data.id);
                    waiter.reject(new Error(`${waiter.operationType} failed`));
                }
                // Otherwise, ignore and wait for another event
            } catch (err) {
                // If parsing/fetching fails, do not break others; log and continue
                console.error('WebSocket event handling error:', err);
            }
        };

        // Bind transaction events
        const binders = [
            'platform:transaction-created',
            'platform:transaction-updated',
        ];
        binders.forEach(ev => enjinChannel.bind(ev, payload => handleEvent(ev, payload)));

        // //Optional: you may use global binding (covers any event variants, not only platform:transaction-created and platform:transaction-updated)
        // if (typeof enjinPusher.bind_global === 'function') {
        //     enjinPusher.bind_global((eventName, payload) => handleEvent(eventName, payload));
        // }

        return true;
    })();

    return wsInitPromise;
}

async function getManagedWallet(externalId){
    const response = await axios.post(process.env.ENJIN_API_URL, {
        query: `query GetWallet($externalId: String!){
  GetWallet(externalId: $externalId){
    account{
      publicKey
      address
    }
  }
}`,
        variables: {
            externalId: externalId
        }
    }, {
        headers: {
            'Content-Type': 'application/json',
            'Authorization': process.env.ENJIN_API_KEY
        }
    });

    return response.data.data.GetWallet;
}

async function getManagedWalletTokens(externalId, after = "") {
    let allTokenAccounts = [];
    let hasNextPage = true;
    let endCursor = after;
    let account = null;

    while (hasNextPage) {
        const response = await axios.post(process.env.ENJIN_API_URL, {
            query: `query GetWalletTokens($externalId: String!){
  GetWallet(externalId: $externalId){
    account{
      publicKey
      address
    }
    tokenAccounts(after: "${endCursor}"){
      pageInfo{
        hasNextPage
        endCursor
      }
      edges{
        node{
          balance
          token{
            tokenId
            collection{
              collectionId
            }
            attributes{
              key
              value
            }
          }
        }
      }
    }
  }
}`,
            variables: {
                externalId: externalId
            }
        }, {
            headers: {
                'Content-Type': 'application/json',
                'Authorization': process.env.ENJIN_API_KEY
            }
        });

        const walletData = response.data.data.GetWallet;
        if (!walletData || !walletData.tokenAccounts) {
            console.warn("No token accounts found in the response.");
            hasNextPage = false;
            break;
        }

        account = walletData.account
        allTokenAccounts = allTokenAccounts.concat(walletData.tokenAccounts.edges.map(edge => edge.node));
        hasNextPage = walletData.tokenAccounts.pageInfo.hasNextPage;
        endCursor = walletData.tokenAccounts.pageInfo.endCursor;
    }

    return {account: account, tokens: allTokenAccounts};
}

async function createManagedWallet(externalId){
    try {
        const existingWallet = await getManagedWallet(externalId);
        if (existingWallet) {
            console.log(`Managed wallet with external ID ${externalId} already exists.`);
            return existingWallet;
        }
        const response = await axios.post(process.env.ENJIN_API_URL, {
            query: `mutation CreateWallet($externalId: String!){
  CreateWallet(externalId: $externalId)
}`,
            variables: {
                externalId: externalId
            }
        }, {
            headers: {
                'Content-Type': 'application/json',
                'Authorization': process.env.ENJIN_API_KEY
            }
        });

        
        const maxRetries = 10;
        const retryInterval = 1000;

        for (let attempt = 1; attempt <= maxRetries; attempt++) {
            const wallet = await getManagedWallet(externalId);
            if (wallet && wallet.account && wallet.account.address) {
                return wallet; // Wallet found, return it.
            }

            // If wallet not found, wait before the next attempt.
            await sleep(retryInterval);
        }

        // If the loop completes without finding the wallet, handle the failure.
        console.error(`Failed to retrieve wallet for ${externalId} after ${maxRetries} attempts.`);
        return null;
        
    } catch (error) {
        console.error(`Failed to create managed wallet with external ID ${externalId}:`, error);
        throw error;
    }
    
}

async function mintToken(tokenId, amount, recipient){
    collectionId = process.env.ENJIN_COLLECTION_ID;
    const response = await axios.post(process.env.ENJIN_API_URL, {
        query: `mutation mintToken(
  $recipient: String!
  $collectionId: BigInt!
  $tokenId: BigInt
  $amount: BigInt!
){
  MintToken(
    recipient: $recipient
    collectionId: $collectionId
    params: {
      tokenId: {integer: $tokenId}
      amount: $amount
    }
  ){
    id
    method
    state
  }
}`,
        variables: {
            recipient: recipient,
            collectionId: collectionId,
            tokenId: tokenId,
            amount: amount
        }
    }, {
        headers: {
            'Content-Type': 'application/json',
            'Authorization': process.env.ENJIN_API_KEY
        }
    });

    return response;
}

async function mintTokenAndWaitForTransaction(tokenId, amount, recipient) {
    console.log('Minting Token, please wait...');
    try {
        const mintTokenResponse = await mintToken(tokenId, amount, recipient);
        const requestId = mintTokenResponse.data.data.MintToken.id;
        await waitForTransaction(requestId, "'Token #" + tokenId + "' minting");
        console.log(`'Token #${tokenId}' minted successfully.`);
        return;
    } catch (error) {
        console.error("Failed to mint 'token #" + tokenId + "': ", JSON.stringify(error));
        throw error;
    }
}

async function meltToken(tokenId, amount, signingAccount){
    collectionId = process.env.ENJIN_COLLECTION_ID;
    const response = await axios.post(process.env.ENJIN_API_URL, {
                query: `mutation burnToken(
  $collectionId: BigInt!
  $tokenId: BigInt
  $amount: BigInt!
  $signingAccount: String!
){
  Burn(
    collectionId: $collectionId
    params: {
      tokenId: {integer: $tokenId}
      amount: $amount
    }
    signingAccount: $signingAccount
  ){
    id
    method
    state
  }
}`,
        variables: {
            collectionId: collectionId,
            tokenId: tokenId,
            amount: amount,
            signingAccount: signingAccount
        }
    }, {
        headers: {
            'Content-Type': 'application/json',
            'Authorization': process.env.ENJIN_API_KEY
        }
    });

    return response;
}

async function meltTokenAndWaitForTransaction(tokenId, amount, signingAccount) {
    console.log('Melting Token, please wait...');
    try {
        const meltTokenResponse = await meltToken(tokenId, amount, signingAccount);
        const requestId = meltTokenResponse.data.data.Burn.id;
        await waitForTransaction(requestId, "'Token #" + tokenId + "' melting");
        console.log(`'Token #${tokenId}' melted successfully.`);
        return;
    } catch (error) {
        console.error("Failed to melt 'token #" + tokenId + "': ", JSON.stringify(error));
        throw error;
    }
}

async function transferToken(tokenId, amount, signingAccount, recipient){
    collectionId = process.env.ENJIN_COLLECTION_ID;
    const response = await axios.post(process.env.ENJIN_API_URL, {
                query: `mutation transferToken(
  $collectionId: BigInt!
  $tokenId: BigInt!
  $amount: BigInt!
  $recipient: String!
  $signingAccount: String!
){
  SimpleTransferToken(
    collectionId: $collectionId
    recipient: $recipient
    params: {
      tokenId: {integer: $tokenId}
      amount: $amount
    }
    signingAccount: $signingAccount
  ){
    id
    method
    state
  }
}`,
        variables: {
            recipient: recipient,
            collectionId: collectionId,
            tokenId: tokenId,
            amount: amount,
            signingAccount: signingAccount
        }
    }, {
        headers: {
            'Content-Type': 'application/json',
            'Authorization': process.env.ENJIN_API_KEY
        }
    });

    return response;
}

async function transferTokenAndWaitForTransaction(tokenId, amount, signingAccount, recipient) {
    console.log('Transferring Token, please wait...');
    try {
        const transferTokenResponse = await transferToken(tokenId, amount, signingAccount, recipient);
        const requestId = transferTokenResponse.data.data.SimpleTransferToken.id;
        await waitForTransaction(requestId, "'Token #" + tokenId + "' transfer");
        console.log(`'Token #${tokenId}' transferred successfully.`);
        return;
    } catch (error) {
        console.error("Failed to transfer 'token #" + tokenId + "': ", JSON.stringify(error));
        throw error;
    }
}

async function waitForTransaction(requestId, operationType = 'operation') {
    await ensureWebsocketInitialized();

    // If a waiter already exists, chain to it
    if (transactionWaiters.has(requestId)) {
        const existing = transactionWaiters.get(requestId);
        return new Promise((resolve, reject) => {
            const prevResolve = existing.resolve;
            const prevReject = existing.reject;
            existing.resolve = (tx) => { try { prevResolve(tx); } catch {} resolve(tx); };
            existing.reject = (err) => { try { prevReject(err); } catch {} reject(err); };
        });
    }

    return new Promise((resolve, reject) => {
        transactionWaiters.set(requestId, { resolve, reject, operationType });
        console.log(`Waiting for the ${operationType} to finalize via WebSocket. Request ID: ${requestId}`);
    });
}

async function checkAndCreateCollection() {
    if (!process.env.ENJIN_COLLECTION_ID) {
        console.log('No collection ID found. Creating new collection, please wait...');
        try {
            const createCollectionResponse = await createCollection();
            const requestId = createCollectionResponse.data.data.CreateCollection.id;
            const transaction = await waitForTransaction(requestId, "'Enjin Sample Game' collection creation");
            
            const collectionId = await extractCollectionId(transaction);
            if (collectionId) {
                await updateEnvFile(collectionId);
                console.log(`Created new collection with ID: ${collectionId}.`);
                return collectionId;
            }
            throw new Error('Failed to extract collection ID');
        } catch (error) {
            console.error('Failed to create collection:', error);
            throw error;
        }
    }
    return process.env.ENJIN_COLLECTION_ID;
}

async function checkTokenExists(collectionId, tokenId) {
    try {
        await getToken(collectionId, tokenId);
        return true;
    } catch (error) {
        if (error.response && error.response.status === 400) {
            return false;
        }
        throw error;
    }
}

async function createResourceToken(collectionId, tokenId, name, media) {
    console.log(`Creating resource token '${name}', please wait...`);
    const createTokenResponse = await createToken(collectionId, tokenId, name, media);
    const requestId = createTokenResponse.data.data.CreateToken.id;
    await waitForTransaction(requestId, `'${name}' token creation`);
    console.log(`Resource token '${name}' created successfully.`);
}

async function prepareCollection() {
    // Ensure collection exists
    const collectionId = await checkAndCreateCollection();

    // Checks which resource tokens exist
    const tokenExistsChecks = await Promise.all(
        RESOURCE_TOKENS.map(token =>
            checkTokenExists(collectionId, token.id)
                .then(exists => ({ ...token, exists }))
        )
    );

    // Create missing tokens in parallel
    const tokensToCreate = tokenExistsChecks.filter(token => !token.exists);
    if (tokensToCreate.length > 0) {
        await Promise.all(
            tokensToCreate.map(token =>
                createResourceToken(collectionId, token.id, token.name, token.media)
                    .catch(error => {
                        console.error(`Failed to create resource token #${token.id}:`, error);
                        throw error;
                    })
            )
        );
    }
}

module.exports = {
    prepareCollection,
    getWalletPublicKey,
    createManagedWallet,
    getManagedWallet,
    getManagedWalletTokens,
    mintTokenAndWaitForTransaction,
    meltTokenAndWaitForTransaction,
    transferTokenAndWaitForTransaction
};
