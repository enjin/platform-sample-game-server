const bcrypt = require('bcryptjs');
const jwt = require('jsonwebtoken');
const User = require('../models/user');

class AuthService {
    static async hashPassword(password) {
        const salt = await bcrypt.genSalt(10);
        return bcrypt.hash(password, salt);
    }

    static async comparePasswords(password, hashedPassword) {
        return bcrypt.compare(password, hashedPassword);
    }

    static generateToken(user) {
        return jwt.sign(
            { email: user.email },
            process.env.JWT_SECRET,
            { expiresIn: '24h' }
        );
    }

    static async register(email, password) {
        if (!email || !password) {
            throw new Error('Email and password are required');
        }

        const existingUser = User.findByEmail(email);
        if (existingUser) {
            // User already exists, try to login
            try {
                const token = await this.login(email, password);
                return token;
            } catch (loginError) {
                throw new Error('Registration failed: User already exists, but login failed.');
            }
        }

        const hashedPassword = await this.hashPassword(password);
        const user = User.create(email, hashedPassword);
        const token = this.generateToken(user);

        return token;
    }

    static async login(email, password) {
        const user = User.findByEmail(email);
        
        if (!user) {
            throw new Error('User not found');
        }

        const isValidPassword = await this.comparePasswords(password, user.password);
        
        if (!isValidPassword) {
            throw new Error('Invalid password');
        }

        const token = this.generateToken(user);
        return token;
    }
}

module.exports = AuthService;