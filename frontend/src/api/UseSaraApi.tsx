import { useContext, useMemo } from 'react'
import { AuthContext } from 'components/Contexts/AuthContext'
import { BackendAPICaller } from './ApiCaller'
import { config } from 'config'
import { SaraApi } from './SaraApi'

export function useSaraApi() {
    const { getSaraAccessToken } = useContext(AuthContext)

    return useMemo(() => {
        const api = new BackendAPICaller(getSaraAccessToken, config.SARA_URL)
        return new SaraApi(api)
    }, [getSaraAccessToken])
}
