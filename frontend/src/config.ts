const globalVars = window as any

const getEnvVariable = (name: string): string => {
    const value = process.env[name]
    if (value === undefined) {
        console.warn('Environment variable named "' + name + '" undefined. Attempting to use global variable.')
        const globalValue = globalVars[name]

        // If global value equals its placeholder value 'placeholderValue', it is considered undefined
        const placeholderValue: string = '${' + name + '}'

        if (globalValue === placeholderValue || globalValue === undefined)
            throw new Error(
                `Global variable "${name}" is not set. Verify that your .env file is up to date with .env.example`
            )
        else return globalValue as string
    } else return value as string
}

export const config = {
    AI_CONNECTION_STRING: getEnvVariable('REACT_APP_AI_CONNECTION_STRING'),
    BACKEND_URL: getEnvVariable('REACT_APP_BACKEND_URL'),
    BACKEND_API_SCOPE: getEnvVariable('REACT_APP_BACKEND_API_SCOPE'),
    BACKEND_API_SIGNALR_URL: getEnvVariable('REACT_APP_BACKEND_URL') + '/hub',
    FRONTEND_URL: getEnvVariable('REACT_APP_FRONTEND_URL'),
    FRONTEND_BASE_ROUTE: getEnvVariable('REACT_APP_FRONTEND_BASE_ROUTE'),
    AD_CLIENT_ID: getEnvVariable('REACT_APP_AD_CLIENT_ID'),
    AD_TENANT_ID: getEnvVariable('REACT_APP_AD_TENANT_ID'),
}
