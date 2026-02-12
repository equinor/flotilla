import { useContext, useMemo } from 'react'
import { BackendApi } from './BackendApi'
import { AuthContext } from 'components/Contexts/AuthContext'
import { AssetContext } from 'components/Contexts/AssetContext'
import { BackendAPICaller } from './ApiCaller'

export function useBackendApi() {
    const { getAccessToken } = useContext(AuthContext)
    const { installationCode } = useContext(AssetContext)

    return useMemo(() => {
        const api = new BackendAPICaller(getAccessToken)
        return new BackendApi(api, installationCode ?? null)
    }, [getAccessToken, installationCode])
}
