import { DefaultPage } from 'components/Pages/DefaultPage/DefaultPage'
import { MissionControlSection } from 'components/Pages/FrontPage/MissionOverview/MissionControlSection'
import { InspectionSection } from 'components/Pages/InspectionPage/InspectionSection'
import { InspectionOverviewSection } from 'components/Pages/InspectionPage/InspectionOverview'
import { MissionHistoryView } from 'components/Pages/MissionHistory/MissionHistoryView'
import { AutoScheduleSection } from 'components/Pages/FrontPage/AutoScheduleSection/AutoScheduleSection'
import { RobotStatusSection } from 'components/Pages/RobotCards/RobotStatusSection'

export const MissionControlPage = () => {
    return (
        <DefaultPage pageName="missionControl">
            <MissionControlSection />
        </DefaultPage>
    )
}

export const AreaOverviewPage = () => {
    return (
        <DefaultPage pageName="areaOverview'">
            <InspectionSection />
        </DefaultPage>
    )
}

export const PredefinedMissionsPage = () => {
    return (
        <DefaultPage pageName="predefinedMissions">
            <InspectionOverviewSection />
        </DefaultPage>
    )
}

export const MissionHistoryPage = () => {
    return (
        <DefaultPage pageName="history">
            <MissionHistoryView refreshInterval={1000} />
        </DefaultPage>
    )
}

export const AutoSchedulePage = () => {
    return (
        <DefaultPage pageName="autoSchedule">
            <AutoScheduleSection />
        </DefaultPage>
    )
}

export const RobotStatusPage = () => {
    return (
        <DefaultPage pageName="robots">
            <RobotStatusSection />
        </DefaultPage>
    )
}
