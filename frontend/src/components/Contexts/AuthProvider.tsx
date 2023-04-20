import { useMsal } from '@azure/msal-react'
import { fetchAccessToken } from 'api/AuthConfig'
import { createContext, useContext, useEffect, useState } from 'react'
import { useErrorHandler } from 'react-error-boundary'

type Props = {
    children?: React.ReactNode
}

export const AuthContext = createContext('')

// Check for new token every second (Will refresh token if needed)
export const tokenReverificationInterval: number = 1000

export const AuthProvider = (props: Props) => {
    const handleError = useErrorHandler()
    const msalContext = useMsal()
    const [accessToken, setAccessToken] = useState('')
    const VerifyToken = () => {
        fetchAccessToken(msalContext).then((accessToken) => {
            setAccessToken(accessToken)
        })
    }
    useEffect(() => {
        const id = setInterval(() => {
            VerifyToken()
        }, tokenReverificationInterval)
        return () => clearInterval(id)
    }, [])

    return <AuthContext.Provider value={accessToken}>{props.children}</AuthContext.Provider>
}
