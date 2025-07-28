const jwt = require('jsonwebtoken');
const User = require('../models/user');

const jwtAuth = (req, res, next) => {
    const authHeader = req.headers["authorization"];

    if (!authHeader) {
        return res.status(401).json({
            success: false,
            message: 'Authorization header is required'
        });
    }

    try {
        const token = authHeader.split(' ')[1]; // Format: "Bearer token"
        const decoded = jwt.verify(token, process.env.JWT_SECRET);
        
        const user = User.findByEmail(decoded.email);
        if (!user) {
            throw new Error('User not found');
        }

        req.user = user;
        next();
    } catch (error) {
        return res.status(401).json({
            success: false,
            message: 'Invalid token'
        });
    }
};

module.exports = jwtAuth;