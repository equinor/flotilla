import { useMsal } from '@azure/msal-react'
import { fetchAccessToken } from 'api/AuthConfig'
import { createContext, useCallback, useEffect, useState } from 'react'

type Props = {
    children?: React.ReactNode
}

const defaultAuthState = {
    accessToken: undefined,
}

interface IAuthContext {
    accessToken: string | undefined
}

export const AuthContext = createContext<IAuthContext>(defaultAuthState)

// Check for new token every second (Will refresh token if needed)
export const tokenReverificationInterval: number = 1000

export const AuthProvider = (props: Props) => {
    const msalContext = useMsal()
    const [accessToken, setAccessToken] = useState<string | undefined>(undefined)

    const VerifyToken = useCallback(() => {
        fetchAccessToken(msalContext)
            .then((accessToken) => {
                setAccessToken(accessToken)
            })
            .catch((error) => {
                console.error('Failed to fetch access token:', error)
            })
    }, [msalContext])

    useEffect(() => {
        const id = setInterval(() => {
            VerifyToken()
        }, tokenReverificationInterval)
        return () => clearInterval(id)
    }, [VerifyToken])

    return <AuthContext.Provider value={{ accessToken: accessToken }}>{props.children}</AuthContext.Provider>
}
