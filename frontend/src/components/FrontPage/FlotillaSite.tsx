import { useMsal } from '@azure/msal-react'
import { fetchAccessToken } from 'authConfig'
import { createContext, useEffect, useState } from 'react'
import { BrowserRouter, Route, Routes } from 'react-router-dom'
import { FrontPage } from './FrontPage'

export const AccessTokenContext = createContext('')

export function FlotillaSite() {
    const authContext = useMsal()
    const [accessToken, setAccessToken] = useState('')
    useEffect(() => {
        fetchAccessToken(authContext).then((accessToken) => {
            setAccessToken(accessToken)
        })
    }, [])
    return (
        <>
            {accessToken === '' && <>Loading...</>}
            {accessToken !== '' && (
                <>
                    <AccessTokenContext.Provider value={accessToken}>
                        <BrowserRouter>
                            <Routes>
                                <Route path="/" element={<FrontPage />} />
                            </Routes>
                        </BrowserRouter>
                    </AccessTokenContext.Provider>
                </>
            )}
        </>
    )
}
