Generate an SSH key by opening a Git Bash console (this doesn't _have_ to be on the build agent) and running this:

	ssh-keygen -t rsa -b 4096 -C "your@name.com"

Don't use a passphrase (just press `ENTER` when prompted) because TeamCity doesn't support SSH keys with passphrases. If you already have SSH keys present you may want to give them a different name. By default the `id_rsa` and `id_rsa.pub` files will be created in `~/.ssh`. Provide TeamCity with the private key by opening the project settings and selecting _SSH Keys_. Click _Upload SSH Key_, enter a name, and select the `id_rsa` file.

Back in Git Bash, type this to copy the public key to the clipboard:

	clip < ~/.ssh/id_rsa.pub

Open the repository's Settings page within Github and select _Deploy keys_. Add a deploy key and paste the public key into the _Key_ field.

Now TeamCity can be configured to use SSH instead of HTTPS. In the project settings, select the VCS root and paste the SSH clone URL from Github. Further down, change the _Authentication method_ to _Uploaded Key_ and pick the correct key for the _Uploaded Key_ field. Save the page and test it out by manually running the build.
