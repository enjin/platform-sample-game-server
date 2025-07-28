const express = require('express');
const cors = require('cors');
const dotenv = require('dotenv');
const { prepareCollection } = require('./services/enjinService');

// Load environment variables
dotenv.config();

// Initialize express app
const app = express();

// Middleware
app.use(cors());
app.use(express.json());

// Routes
app.use('/api/auth', require('./routes/auth'));
app.use('/api/wallet', require('./routes/wallet'));
app.use('/api/token', require('./routes/token'));

// Basic error handling
app.use((err, req, res, next) => {
    console.error(err.stack);
    res.status(500).json({
        success: false,
        message: 'Internal Server Error'
    });
});

const PORT = process.env.PORT || 3000;

// Start server after checking collection
prepareCollection()
    .then(() => {
        console.log("----------------------------------------")
        console.log(`Collection and resource tokens are ready. Using collection ID: ${process.env.ENJIN_COLLECTION_ID}`);
        const server = app.listen(PORT, () => {
            console.log(`Server is running on port ${PORT}`);
            console.log("----------------------------------------")
        });
        // Set the timeout for all requests to 1 hour (3,600,000 ms)
        server.timeout = 3600000;
    })
    .catch(error => {
        console.error('Failed to start server:', error);
        process.exit(1);
    });