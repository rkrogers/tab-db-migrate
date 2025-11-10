const axios = require('axios');

/**
 * Handles authentication with Tableau Cloud/Server using the REST API
 */
class TableauAuth {
    constructor(serverUrl, apiVersion = '3.21') {
        this.serverUrl = this.cleanServerUrl(serverUrl);
        this.apiVersion = apiVersion;
    }

    /**
     * Cleans and validates the server URL by removing common mistakes
     */
    cleanServerUrl(url) {
        url = url.trim().replace(/\/+$/, '');
        
        // Remove common patterns users might include
        // e.g., "https://10ay.online.tableau.com/#/site/mysite" -> "https://10ay.online.tableau.com"
        if (url.includes('/#/')) {
            url = url.substring(0, url.indexOf('/#/'));
        }
        
        // Remove "/api" if included
        if (url.endsWith('/api')) {
            url = url.substring(0, url.length - 4);
        }
        
        return url;
    }

    /**
     * Authenticates with Tableau using a Personal Access Token (PAT)
     */
    async signInWithPAT(tokenName, tokenSecret, siteName = '') {
        const signInUrl = `${this.serverUrl}/api/${this.apiVersion}/auth/signin`;

        const requestBody = {
            credentials: {
                personalAccessTokenName: tokenName,
                personalAccessTokenSecret: tokenSecret,
                site: {
                    contentUrl: siteName
                }
            }
        };

        try {
            const response = await axios.post(signInUrl, requestBody, {
                headers: {
                    'Content-Type': 'application/json',
                    'Accept': 'application/json'
                }
            });

            const credentials = response.data.credentials;

            if (!credentials || !credentials.token) {
                throw new Error('Invalid response from Tableau API - missing credentials');
            }

            return {
                token: credentials.token,
                siteId: credentials.site?.id || '',
                userId: credentials.user?.id || '',
                serverUrl: this.serverUrl,
                apiVersion: this.apiVersion
            };
        } catch (error) {
            if (error.response) {
                throw new Error(
                    `Tableau PAT authentication failed with status ${error.response.status}: ${JSON.stringify(error.response.data)}`
                );
            }
            throw new Error(`Authentication error: ${error.message}`);
        }
    }

    /**
     * Signs out from Tableau
     */
    async signOut(authToken, siteId) {
        const signOutUrl = `${this.serverUrl}/api/${this.apiVersion}/auth/signout`;

        try {
            await axios.post(signOutUrl, {}, {
                headers: {
                    'X-Tableau-Auth': authToken,
                    'Accept': 'application/json'
                }
            });
        } catch (error) {
            if (error.response) {
                throw new Error(
                    `Tableau sign out failed with status ${error.response.status}: ${JSON.stringify(error.response.data)}`
                );
            }
            throw new Error(`Sign out error: ${error.message}`);
        }
    }
}

module.exports = TableauAuth;
