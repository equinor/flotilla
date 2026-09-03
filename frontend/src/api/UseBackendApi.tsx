import { useContext, useMemo } from 'react'
import { BackendApi } from './BackendApi'
import { AuthContext } from 'contexts/AuthContext'
import { BackendAPICaller } from './ApiCaller'
import { config } from 'config'

export function useBackendApi() {
    const { getBackendAccessToken } = useContext(AuthContext)

    return useMemo(() => {
        const api = new BackendAPICaller(getBackendAccessToken, config.BACKEND_URL)
        return new BackendApi(api)
    }, [getBackendAccessToken])
}
