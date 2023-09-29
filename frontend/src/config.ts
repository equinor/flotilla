const globalVars = window as any

function GetEnvVariable(name: string): string {
    const value = process.env[name]
    if (value === undefined) {
        console.warn('Environment variable named "' + name + '" undefined. Attempting to use global variable.')
        const globalValue = globalVars[name]

        // If global value equals its placeholder value 'placeholderValue', it is considered undefined
        const placeholderValue: string = '${' + name + '}'

        if (globalValue === placeholderValue || !globalValue)
            throw new Error(
                `Global variable "${name}" is not set. Verify that your .env file is up to date with .env.example`
            )
        else return globalValue as string
    } else return value as string
}

export const config = {
    BACKEND_URL: GetEnvVariable('REACT_APP_BACKEND_URL'),
    BACKEND_API_SCOPE: GetEnvVariable('REACT_APP_BACKEND_API_SCOPE'),
    BACKEND_API_SIGNALR_URL: GetEnvVariable('REACT_APP_BACKEND_URL') + '/hub',
    FRONTEND_URL: GetEnvVariable('REACT_APP_FRONTEND_URL'),
    FRONTEND_BASE_ROUTE: GetEnvVariable('REACT_APP_FRONTEND_BASE_ROUTE'),
    AD_CLIENT_ID: GetEnvVariable('REACT_APP_AD_CLIENT_ID'),
    AD_TENANT_ID: GetEnvVariable('REACT_APP_AD_TENANT_ID'),
}
