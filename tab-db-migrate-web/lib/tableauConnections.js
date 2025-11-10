const axios = require('axios');

/**
 * Handles listing and managing data source and workbook connections from Tableau
 */
class TableauConnections {
    constructor(serverUrl, apiVersion = '3.21') {
        this.serverUrl = this.cleanServerUrl(serverUrl);
        this.apiVersion = apiVersion;
    }

    /**
     * Cleans and validates the server URL
     */
    cleanServerUrl(url) {
        url = url.trim().replace(/\/+$/, '');
        if (url.includes('/#/')) {
            url = url.substring(0, url.indexOf('/#/'));
        }
        if (url.endsWith('/api')) {
            url = url.substring(0, url.length - 4);
        }
        return url;
    }

    /**
     * Enumerates all data sources and their connections
     */
    async enumerateDataSources(authToken, siteId) {
        const dataSources = await this.queryDataSources(authToken, siteId);
        console.log(`Found ${dataSources.length} data sources on the site.`);

        const dataSourcesWithConnections = [];

        for (const dataSource of dataSources) {
            console.log(`Querying connections for data source: ${dataSource.name} (ID: ${dataSource.id})`);
            
            const connections = await this.queryDataSourceConnections(authToken, siteId, dataSource.id);
            
            dataSourcesWithConnections.push({
                id: dataSource.id,
                name: dataSource.name,
                contentUrl: dataSource.contentUrl || '',
                type: dataSource.type || '',
                projectName: dataSource.project?.name || '',
                connections: connections.map(conn => ({
                    ...conn,
                    sourceType: 'datasource',
                    parentId: dataSource.id,
                    parentName: dataSource.name
                }))
            });
        }

        return dataSourcesWithConnections;
    }

    /**
     * Enumerates all workbooks and their connections
     */
    async enumerateWorkbooks(authToken, siteId) {
        const workbooks = await this.queryWorkbooks(authToken, siteId);
        console.log(`Found ${workbooks.length} workbooks on the site.`);

        const workbooksWithConnections = [];

        for (const workbook of workbooks) {
            console.log(`Querying connections for workbook: ${workbook.name} (ID: ${workbook.id})`);
            
            const connections = await this.queryWorkbookConnections(authToken, siteId, workbook.id);
            
            workbooksWithConnections.push({
                id: workbook.id,
                name: workbook.name,
                contentUrl: workbook.contentUrl || '',
                projectName: workbook.project?.name || '',
                connections: connections.map(conn => ({
                    ...conn,
                    sourceType: 'workbook',
                    parentId: workbook.id,
                    parentName: workbook.name
                }))
            });
        }

        return workbooksWithConnections;
    }

    /**
     * Query all data sources on the site
     */
    async queryDataSources(authToken, siteId) {
        const url = `${this.serverUrl}/api/${this.apiVersion}/sites/${siteId}/datasources`;
        
        try {
            const response = await axios.get(url, {
                headers: {
                    'X-Tableau-Auth': authToken,
                    'Accept': 'application/json'
                }
            });

            return response.data.datasources?.datasource || [];
        } catch (error) {
            throw new Error(`Failed to query data sources: ${error.message}`);
        }
    }

    /**
     * Query all workbooks on the site
     */
    async queryWorkbooks(authToken, siteId) {
        const url = `${this.serverUrl}/api/${this.apiVersion}/sites/${siteId}/workbooks`;
        
        try {
            const response = await axios.get(url, {
                headers: {
                    'X-Tableau-Auth': authToken,
                    'Accept': 'application/json'
                }
            });

            return response.data.workbooks?.workbook || [];
        } catch (error) {
            throw new Error(`Failed to query workbooks: ${error.message}`);
        }
    }

    /**
     * Query connections for a specific data source
     */
    async queryDataSourceConnections(authToken, siteId, dataSourceId) {
        const url = `${this.serverUrl}/api/${this.apiVersion}/sites/${siteId}/datasources/${dataSourceId}/connections`;
        
        try {
            const response = await axios.get(url, {
                headers: {
                    'X-Tableau-Auth': authToken,
                    'Accept': 'application/json'
                }
            });

            const connections = response.data.connections?.connection || [];
            return connections.map(conn => ({
                id: conn.id,
                type: conn.type || '',
                serverAddress: conn.serverAddress || '',
                serverPort: conn.serverPort || '',
                userName: conn.userName || ''
            }));
        } catch (error) {
            console.log(`Warning: Failed to query connections for data source ${dataSourceId}. Status: ${error.response?.status}`);
            return [];
        }
    }

    /**
     * Query connections for a specific workbook
     */
    async queryWorkbookConnections(authToken, siteId, workbookId) {
        const url = `${this.serverUrl}/api/${this.apiVersion}/sites/${siteId}/workbooks/${workbookId}/connections`;
        
        try {
            const response = await axios.get(url, {
                headers: {
                    'X-Tableau-Auth': authToken,
                    'Accept': 'application/json'
                }
            });

            const connections = response.data.connections?.connection || [];
            return connections.map(conn => ({
                id: conn.id,
                type: conn.type || '',
                serverAddress: conn.serverAddress || '',
                serverPort: conn.serverPort || '',
                userName: conn.userName || ''
            }));
        } catch (error) {
            console.log(`Warning: Failed to query connections for workbook ${workbookId}. Status: ${error.response?.status}`);
            return [];
        }
    }

    /**
     * Group unique connections from data sources and workbooks
     */
    groupUniqueConnections(dataSources, workbooks) {
        const connectionMap = new Map();

        // Process data sources
        for (const ds of dataSources) {
            for (const conn of ds.connections) {
                const key = `${conn.serverAddress}:${conn.serverPort}:${conn.userName}`;
                
                if (!connectionMap.has(key)) {
                    connectionMap.set(key, {
                        serverAddress: conn.serverAddress,
                        serverPort: conn.serverPort,
                        userName: conn.userName,
                        dataSources: [],
                        workbooks: [],
                        connections: []
                    });
                }

                const group = connectionMap.get(key);
                group.dataSources.push({
                    id: ds.id,
                    name: ds.name
                });
                group.connections.push(conn);
            }
        }

        // Process workbooks
        for (const wb of workbooks) {
            for (const conn of wb.connections) {
                const key = `${conn.serverAddress}:${conn.serverPort}:${conn.userName}`;
                
                if (!connectionMap.has(key)) {
                    connectionMap.set(key, {
                        serverAddress: conn.serverAddress,
                        serverPort: conn.serverPort,
                        userName: conn.userName,
                        dataSources: [],
                        workbooks: [],
                        connections: []
                    });
                }

                const group = connectionMap.get(key);
                group.workbooks.push({
                    id: wb.id,
                    name: wb.name
                });
                group.connections.push(conn);
            }
        }

        return Array.from(connectionMap.values()).map((group, index) => ({
            id: index + 1,
            ...group,
            totalConnections: group.connections.length,
            dataSourceCount: group.dataSources.length,
            workbookCount: group.workbooks.length
        }));
    }

    /**
     * Update connections for a group of data sources and workbooks
     */
    async updateConnections(authToken, siteId, connections, serverAddress, serverPort, userName, password) {
        const results = {
            successful: [],
            failed: []
        };

        for (const conn of connections) {
            try {
                let success = false;

                if (conn.sourceType === 'datasource') {
                    success = await this.updateDataSourceConnection(
                        authToken, siteId, conn.parentId, conn.id,
                        serverAddress, serverPort, userName, password
                    );
                } else if (conn.sourceType === 'workbook') {
                    success = await this.updateWorkbookConnection(
                        authToken, siteId, conn.parentId, conn.id,
                        serverAddress, serverPort, userName, password
                    );
                }

                if (success) {
                    results.successful.push({
                        name: conn.parentName,
                        type: conn.sourceType
                    });
                } else {
                    results.failed.push({
                        name: conn.parentName,
                        type: conn.sourceType,
                        error: 'Update failed'
                    });
                }
            } catch (error) {
                results.failed.push({
                    name: conn.parentName,
                    type: conn.sourceType,
                    error: error.message
                });
            }
        }

        return results;
    }

    /**
     * Update a data source connection
     */
    async updateDataSourceConnection(authToken, siteId, dataSourceId, connectionId, serverAddress, serverPort, userName, password) {
        const url = `${this.serverUrl}/api/${this.apiVersion}/sites/${siteId}/datasources/${dataSourceId}/connections/${connectionId}`;

        const requestBody = {
            connection: {
                serverAddress,
                serverPort,
                userName,
                password
            }
        };

        try {
            await axios.put(url, requestBody, {
                headers: {
                    'X-Tableau-Auth': authToken,
                    'Content-Type': 'application/json',
                    'Accept': 'application/json'
                }
            });

            console.log('✓ Data source connection updated successfully!');
            return true;
        } catch (error) {
            console.log(`✗ Failed to update data source connection. Status: ${error.response?.status}`);
            return false;
        }
    }

    /**
     * Update a workbook connection
     */
    async updateWorkbookConnection(authToken, siteId, workbookId, connectionId, serverAddress, serverPort, userName, password) {
        const url = `${this.serverUrl}/api/${this.apiVersion}/sites/${siteId}/workbooks/${workbookId}/connections/${connectionId}`;

        const requestBody = {
            connection: {
                serverAddress,
                serverPort,
                userName,
                password
            }
        };

        try {
            await axios.put(url, requestBody, {
                headers: {
                    'X-Tableau-Auth': authToken,
                    'Content-Type': 'application/json',
                    'Accept': 'application/json'
                }
            });

            console.log('✓ Workbook connection updated successfully!');
            return true;
        } catch (error) {
            console.log(`✗ Failed to update workbook connection. Status: ${error.response?.status}`);
            return false;
        }
    }
}

module.exports = TableauConnections;
