const express = require('express');
const router = express.Router();
const axios = require('axios');
const jwtAuth = require('../middlewares/jwtAuth');
const { getManagedWallet, createManagedWallet, getManagedWalletTokens } = require('../services/enjinService');

// Get managed wallet endpoint
router.post('/get', jwtAuth, async (req, res) => {
    try {
        const userEmail = req.user.email;
        const getManagedWalletResponse = await getManagedWallet(userEmail)
        if (!getManagedWalletResponse)
            throw new Error(`Failed to get managed wallet for external id ${userEmail}`);
        const wallet = getManagedWalletResponse.account.address;
        res.json({
            success: true,
            wallet: wallet
        });
    } catch (error) {
        res.status(500).json({
            success: false,
            message: error.message
        });
    }
});

// Create and return managed wallet endpoint
router.post('/create', jwtAuth, async (req, res) => {
    try {
        const userEmail = req.user.email;
        const createManagedWalletResponse = await createManagedWallet(userEmail)
        if (!createManagedWalletResponse)
            throw new Error(`Failed to get managed wallet for external id ${userEmail}`);
        const wallet = createManagedWalletResponse.account.address;
        res.json({
            success: true,
            wallet: wallet
        });
    } catch (error) {
        res.status(500).json({
            success: false,
            message: error.message
        });
    }
});

// Get managed wallet endpoint
router.get('/get-tokens', jwtAuth, async (req, res) => {
    try {
        const userEmail = req.user.email;
        const getManagedWalletTokensResponse = await getManagedWalletTokens(userEmail)
        if (!getManagedWalletTokensResponse)
            throw new Error(`Failed to get managed wallet tokens for external id ${userEmail}`);
        res.json({
            "account": getManagedWalletTokensResponse.account,
            "tokenAccounts": getManagedWalletTokensResponse.tokens
        });
    } catch (error) {
        res.status(500).json({
            success: false,
            message: error.message
        });
    }
});

module.exports = router;