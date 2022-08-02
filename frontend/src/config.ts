const globalVars = window as any

function GetEnvVariable(name: string): string {
    const value = globalVars[name]
    if (value === '' || value === undefined) {
        console.warn('Global variable named "' + name + '" undefined or empty. Attempting to use env variable.')
        const env_value = process.env[name]

        if (env_value === undefined)
            throw new Error(
                `Env variable "${name}" is not set. Verify that your .env file is up to date with .env.example`
            )
        else return env_value as string
    } else return value as string
}

export const config = {
    BACKEND_URL: GetEnvVariable('BACKEND_URL'),
    FRONTEND_URL: GetEnvVariable('FRONTEND_URL'),
    AD_CLIENT_ID: GetEnvVariable('AD_CLIENT_ID'),
    AD_TENANT_ID: GetEnvVariable('AD_TENANT_ID'),
}
