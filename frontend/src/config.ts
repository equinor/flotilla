
function GetEnvVariable(name: string): string
{
    const value = process.env[name]

    if (value === undefined)
        throw new Error(`Env variable "${name}" is not set. Verify that your .env file is up to date with .env.example`)
    else
        return value as string
}

export const config = {
    BACKEND_URL: GetEnvVariable("REACT_APP_BACKEND_URL"),
    FRONTEND_URL: GetEnvVariable("REACT_APP_FRONTEND_URL"),
    AD_CLIENT_ID: GetEnvVariable("REACT_APP_AD_CLIENT_ID"),
    AD_TENANT_ID: GetEnvVariable("REACT_APP_AD_TENANT_ID"),
}