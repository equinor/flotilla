import { useQuery } from '@tanstack/react-query'
import { useBackendApi } from 'api/UseBackendApi'
import { RobotStatistics } from 'models/RobotStatistics'

const SECONDS_PER_HOUR = 60 * 60
const SECONDS_PER_DAY = 24 * SECONDS_PER_HOUR
const STATISTICS_WINDOW_DAYS = 30

// Bucket "now" to the top of the current hour so the query window - and thus the
// query key - stays stable across renders instead of changing every render tick.
const currentWindow = () => {
    const nowSeconds = Math.floor(Date.now() / 1000)
    const maxCreationTime = nowSeconds - (nowSeconds % SECONDS_PER_HOUR)
    const minCreationTime = maxCreationTime - STATISTICS_WINDOW_DAYS * SECONDS_PER_DAY
    return { minCreationTime, maxCreationTime }
}

export const useRobotStatistics = (robotId: string, enabled: boolean = true) => {
    const backendApi = useBackendApi()
    const { minCreationTime, maxCreationTime } = currentWindow()

    return useQuery<RobotStatistics>({
        queryKey: ['fetchRobotStatistics', robotId, minCreationTime, maxCreationTime],
        queryFn: async () => backendApi.getRobotStatistics(robotId, minCreationTime, maxCreationTime),
        retry: 1,
        staleTime: 10 * 60 * 1000, // If data is received, stale time is 10 min before making new API call
        enabled: enabled && !!robotId,
    })
}
