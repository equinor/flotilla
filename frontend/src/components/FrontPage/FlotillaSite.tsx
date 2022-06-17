import { useMsal } from '@azure/msal-react'
import { fetchAccessToken } from 'authConfig'
import { createContext, useEffect, useState } from 'react'
import { BrowserRouter, Route, Routes } from 'react-router-dom'
import { FrontPage } from './FrontPage'
import { TestPage } from './TestPage'

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
                                <Route path="test" element={<TestPage />} />
                            </Routes>
                        </BrowserRouter>
                    </AccessTokenContext.Provider>
                </>
            )}
        </>
    )
}
