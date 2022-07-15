
import { useMsal } from '@azure/msal-react'
import { fetchAccessToken } from 'api/AuthConfig'
import { Header } from 'components/Header/Header'
import { createContext, Dispatch, SetStateAction, useContext, useEffect, useState } from 'react'
import { BrowserRouter, Route, Routes } from 'react-router-dom'
import styled from 'styled-components'
import { FrontPage } from './FrontPage'
import { MissionPage } from './MissionPage'

export const AccessTokenContext = createContext('')

const StyledPages = styled.div`
    margin: 2rem;
`

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
                        <Header />
                        <StyledPages>
                            <BrowserRouter>
                                <Routes>
                                    <Route path="/" element={<FrontPage />} />
                                    <Route path="/mission" element={<MissionPage />} />
                                </Routes>
                            </BrowserRouter>
                        </StyledPages>
                    </AccessTokenContext.Provider>
                </>
            )}
        </>
    )
}


// export function AssetState() {
//     return (
//         <AssetContext.Provider value={{ asset, setAsset }}>

//         </AssetContext.Provider>

//     )
// }