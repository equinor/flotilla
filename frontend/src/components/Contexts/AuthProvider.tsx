import { useMsal } from '@azure/msal-react'
import { fetchAccessToken } from 'api/AuthConfig'
import { createContext, useContext, useEffect, useState } from 'react'
import { useErrorHandler } from 'react-error-boundary'

type Props = {
    children?: React.ReactNode
}

export const AuthContext = createContext('')

export const AuthProvider = (props: Props) => {
    // Check for new token every 5 seconds
    const tokenRefreshInterval = 5000
    const handleError = useErrorHandler()
    const msalContext = useMsal()
    const [accessToken, setAccessToken] = useState('')
    const UpdateToken = () => {
        fetchAccessToken(msalContext).then((accessToken) => {
            setAccessToken(accessToken)
        })
    }
    UpdateToken()
    useEffect(() => {
        const id = setInterval(() => {
            UpdateToken()
        }, tokenRefreshInterval)
        return () => clearInterval(id)
    }, [])
    return <AuthContext.Provider value={accessToken}>{props.children}</AuthContext.Provider>
}
