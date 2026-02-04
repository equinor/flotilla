import { useNavigate, useSearchParams } from 'react-router-dom'
import { MissionPage, SimpleMissionPage } from './MissionPage/MissionPage'
import { RobotPage } from './RobotPage/RobotPage'
import { MissionDefinitionPage } from './MissionDefinitionPage/MissionDefinitionPage'
import { config } from 'config'
import { useAssetContext } from 'components/Contexts/AssetContext'

export const SimpleMissionPageRouter = () => {
    // eslint-disable-next-line @typescript-eslint/no-unused-vars
    const [searchParams, setSearchParams] = useSearchParams()

    const id = searchParams.get('id') ?? undefined
    const inspectionId = searchParams.get('inspectionId') ?? undefined
    const analysisId = searchParams.get('analysisId') ?? undefined

    return <SimpleMissionPage missionId={id} inspectionId={inspectionId} analysisId={analysisId} />
}

export const MissionPageRouter = () => {
    // eslint-disable-next-line @typescript-eslint/no-unused-vars
    const [searchParams, setSearchParams] = useSearchParams()

    const id = searchParams.get('id') ?? undefined
    const inspectionId = searchParams.get('inspectionId') ?? undefined
    const analysisId = searchParams.get('analysisId') ?? undefined

    return <MissionPage missionId={id} inspectionId={inspectionId} analysisId={analysisId} />
}

export const MissionDefinitionPageRouter = () => {
    const navigate = useNavigate()
    // eslint-disable-next-line @typescript-eslint/no-unused-vars
    const [searchParams, setSearchParams] = useSearchParams()

    const id = searchParams.get('id') ?? undefined

    if (!id) {
        navigate(`${config.FRONTEND_BASE_ROUTE}/page-not-found`)
        return <></>
    }

    return <MissionDefinitionPage missionId={id} />
}

export const RobotPageRouter = () => {
    const navigate = useNavigate()
    // eslint-disable-next-line @typescript-eslint/no-unused-vars
    const [searchParams, setSearchParams] = useSearchParams()
    const { enabledRobots } = useAssetContext()

    const id = searchParams.get('id') ?? undefined
    const selectedRobot = enabledRobots.find((robot) => robot.id === id)

    if (!id) {
        navigate(`${config.FRONTEND_BASE_ROUTE}/page-not-found`)
        return <></>
    }

    if (!selectedRobot) {
        return <>Loading robot...</>
    }
    return <RobotPage robot={selectedRobot} />
}
