const express = require('express');
const router = express.Router();
const AuthService = require('../services/authService');
const { getManagedWallet, createManagedWallet } = require('../services/enjinService');

// Health check endpoint
router.get('/health-check', async (req, res) => {
    res.status(200).json({
        status: "OK"
    });
});

// Register endpoint
router.post('/register', async (req, res) => {
    try {
        const { email, password } = req.body;
        const token = await AuthService.register(email, password);
        const createManagedWalletResponse = await createManagedWallet(email);
        let wallet = null;
        if (createManagedWalletResponse)
            wallet = createManagedWalletResponse.account.address;
            
        res.status(201).json({
            wallet: wallet,
            token: token
        });
    } catch (error) {
        res.status(400).json({
            success: false,
            message: error.message
        });
    }
});

// Login endpoint
router.post('/login', async (req, res) => {
    try {
        const { email, password } = req.body;
        const token = await AuthService.login(email, password);
        const getManagedWalletResponse = await getManagedWallet(email);
        let wallet = null;
        if (getManagedWalletResponse)
            wallet = getManagedWalletResponse.account.address;
        
        res.status(200).json({
            wallet: wallet,
            token: token
        });
    } catch (error) {
        res.status(401).json({
            success: false,
            message: error.message
        });
    }
});

module.exports = router;