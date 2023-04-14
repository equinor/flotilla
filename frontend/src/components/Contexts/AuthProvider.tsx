import { useMsal } from '@azure/msal-react'
import { fetchAccessToken } from 'api/AuthConfig'
import { createContext, useContext, useEffect, useState } from 'react'
import { useErrorHandler } from 'react-error-boundary'

type Props = {
    children?: React.ReactNode
}

export const AuthContext = createContext('')

export const AuthProvider = (props: Props) => {
    const handleError = useErrorHandler()
    const msalContext = useMsal()
    const [accessToken, setAccessToken] = useState('')
    useEffect(() => {
        fetchAccessToken(msalContext).then((accessToken) => {
            setAccessToken(accessToken)
        })
        //.catch((e) => handleError(e))
    })
    return <AuthContext.Provider value={accessToken}>{props.children}</AuthContext.Provider>
}
