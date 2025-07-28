const appKeyAuth = (req, res, next) => {
    const authHeader = req.headers.authorization;
    
    if (!authHeader) {
        return res.status(401).json({ 
            success: false, 
            message: 'Authorization header is required' 
        });
    }

    if (authHeader !== process.env.APP_KEY) {
        return res.status(401).json({ 
            success: false, 
            message: 'Invalid authorization' 
        });
    }

    next();
};

module.exports = appKeyAuth;