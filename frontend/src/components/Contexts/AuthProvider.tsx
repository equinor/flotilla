import { useMsal } from '@azure/msal-react'
import { fetchAccessToken } from 'api/AuthConfig'
import { createContext, useCallback, useEffect, useState } from 'react'

type Props = {
    children?: React.ReactNode
}

export const AuthContext = createContext('')

// Check for new token every second (Will refresh token if needed)
export const tokenReverificationInterval: number = 1000

export const AuthProvider = (props: Props) => {
    const msalContext = useMsal()
    const [accessToken, setAccessToken] = useState('')

    const VerifyToken = useCallback(() => {
        fetchAccessToken(msalContext).then((accessToken) => {
            setAccessToken(accessToken)
        })
    }, [msalContext])

    useEffect(() => {
        const id = setInterval(() => {
            VerifyToken()
        }, tokenReverificationInterval)
        return () => clearInterval(id)
    }, [VerifyToken])

    return <AuthContext.Provider value={accessToken}>{props.children}</AuthContext.Provider>
}
