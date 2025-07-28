const users = new Map();

class User {
    constructor(email, password) {
        this.email = email;
        this.password = password;
    }

    static findByEmail(email) {
        return users.get(email);
    }

    static create(email, password) {
        if (users.has(email)) {
            throw new Error('User already exists');
        }
        
        const user = new User(email, password);
        users.set(email, user);
        return user;
    }
}

module.exports = User;