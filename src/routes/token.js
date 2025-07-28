const express = require('express');
const router = express.Router();
const axios = require('axios');
const appKeyAuth = require('../middlewares/appKeyAuth');
const jwtAuth = require('../middlewares/jwtAuth');
const { mintTokenAndWaitForTransaction, meltTokenAndWaitForTransaction, transferTokenAndWaitForTransaction, getManagedWallet } = require('../services/enjinService');

// Middleware chain for token operations
const tokenMiddleware = [appKeyAuth, jwtAuth];

// Mint token endpoint
router.post('/mint', tokenMiddleware, async (req, res) => {
    try {
        const userEmail = req.user.email;
        const getManagedWalletResponse = await getManagedWallet(userEmail)
        if (!getManagedWalletResponse)
            throw new Error(`Failed to get managed wallet for external id ${userEmail}`);
        const userWalletAddress = getManagedWalletResponse.account.address;
        await mintTokenAndWaitForTransaction(req.body.tokenId, req.body.amount, userWalletAddress);

        res.json({
            success: true
        });
    } catch (error) {
        res.status(500).json({
            success: false,
            message: error.message
        });
    }
});

// Melt token endpoint
router.post('/melt', tokenMiddleware, async (req, res) => {
    try {
        const userEmail = req.user.email;
        const getManagedWalletResponse = await getManagedWallet(userEmail)
        if (!getManagedWalletResponse)
            throw new Error(`Failed to get managed wallet for external id ${userEmail}`);
        const userWalletAddress = getManagedWalletResponse.account.address;
        await meltTokenAndWaitForTransaction(req.body.tokenId, req.body.amount, userWalletAddress);

        res.json({
            success: true
        });
    } catch (error) {
        res.status(500).json({
            success: false,
            message: error.message
        });
    }
});

// Transfer token endpoint
router.post('/transfer', tokenMiddleware, async (req, res) => {
    try {
        const userEmail = req.user.email;
        const getManagedWalletResponse = await getManagedWallet(userEmail)
        if (!getManagedWalletResponse)
            throw new Error(`Failed to get managed wallet for external id ${userEmail}`);
        const userWalletAddress = getManagedWalletResponse.account.address;
        await transferTokenAndWaitForTransaction(req.body.tokenId, req.body.amount, userWalletAddress, req.body.recipient);

        res.json({
            success: true
        });
    } catch (error) {
        res.status(500).json({
            success: false,
            message: error.message
        });
    }
});

module.exports = router;