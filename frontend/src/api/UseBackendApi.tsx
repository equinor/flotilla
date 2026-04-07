import { useContext, useMemo } from 'react'
import { BackendApi } from './BackendApi'
import { AuthContext } from 'components/Contexts/AuthContext'
import { BackendAPICaller } from './ApiCaller'

export function useBackendApi() {
    const { getAccessToken } = useContext(AuthContext)

    return useMemo(() => {
        const api = new BackendAPICaller(getAccessToken)
        return new BackendApi(api)
    }, [])
}
