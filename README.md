# Sample Server Quickstart

This sample server is built on the Laravel PHP framework and will require Nginx or Apache2, PHP 8.3, Git and Composer as well as MySQL database to store the user account data.

First create a server with your favourite cloud hosting provider, e.g. DigitalOcean to host the Game Server component.  The project is not intensive, and does not require a lot of resources to run so if using DigitalOcean then the minimim spec droplet should be enough for the moment.  After provisioning you'd typically need to ensure it has the correct software stack installed to be able to act as your game server, this would include Nginx or Apache2 webserver, PHP8.3, MySql, Git and Composer.  To help speed up this step and simplify setting up the server software you can use the Laravel Marketplace image: https://marketplace.digitalocean.com/apps/laravel

Once the server is up and running we can pull the Game Server project in and configure it.  For the rest of this guide we'll assume you used the Marketplace Image mentioned above.  Please ensure you have noted down all the passwords and info created during the server setup, including your database username and password as we will need this when setting up the game server.

Start by SSHing into your new server and navigate to the existing laravel folder.

```
cd /var/www/Laravel
```

After making sure you have you database passwords noted down, deleted the entire contents of the folder.

```
rm -r *
```

Next clone the sample game server repo into the folder:

```
git clone git@github.com:enjin/platform-sample-game-server.git .
```

Make a copy of the sample .env file:

```
cp .env.sample .env
```

Install the dependencies with composer:

```
composer install
```

Next generate an app key:

```
php artisan key:generate
```

Edit your .env file, and update the database username and password:

```
DB_CONNECTION=mysql
DB_HOST=127.0.0.1
DB_PORT=3306
DB_DATABASE=sample_game
DB_USERNAME=
DB_PASSWORD=
```

Finally run your migrations:

```
php artisan migrate
```

You are now ready to start configuring the connection to the Blockchain Platform server.  Open the .env file and set the platform URL and auth token, it is recommended to use a strong auth token like a UUID here:

```
PLATFORM_URL=https://platform.canary.enjin.io
PLATFORM_AUTH_TOKEN=
PLATFORM_PRIMARY_SCHEMA=core
```

Set the session guard to 'sanctum':

```
AUTH_GUARD=sanctum
```

And finally create a game key, a UUID is recommended here:

```
GAME_KEY=
```

Save your .env file.  Your game platform is now ready for use.
