import { useNavigate, useParams, useSearchParams } from 'react-router-dom'
import { MissionPage, SimpleMissionPage } from './MissionPage/MissionPage'
import { RobotPage } from './RobotPage/RobotPage'
import { MissionDefinitionPage } from './MissionDefinitionPage/MissionDefinitionPage'
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
    const { missionId } = useParams()

    // eslint-disable-next-line @typescript-eslint/no-unused-vars
    const [searchParams, setSearchParams] = useSearchParams()

    const inspectionId = searchParams.get('inspectionId') ?? undefined
    const analysisId = searchParams.get('analysisId') ?? undefined

    return <MissionPage missionId={missionId} inspectionId={inspectionId} analysisId={analysisId} />
}

export const MissionDefinitionPageRouter = () => {
    const navigate = useNavigate()
    const { missionId } = useParams()

    if (!missionId) {
        navigate(`/not-found`)
        return <></>
    }

    return <MissionDefinitionPage missionId={missionId} />
}

export const RobotPageRouter = () => {
    const navigate = useNavigate()
    const { robotId } = useParams()
    const { enabledRobots } = useAssetContext()

    const selectedRobot = enabledRobots.find((robot) => robot.id === robotId)

    if (!robotId) {
        navigate(`/not-found`)
        return <></>
    }

    if (!selectedRobot) {
        return <>Loading robot...</>
    }
    return <RobotPage robot={selectedRobot} />
}
