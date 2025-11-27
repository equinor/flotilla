import { DefaultPage } from 'pages/DefaultPage/DefaultPage'
import { MissionControlSection } from 'pages/FrontPage/MissionOverview/MissionControlSection'
import { InspectionSection } from 'pages/InspectionPage/InspectionSection'
import { InspectionOverviewSection } from 'pages/InspectionPage/InspectionOverview'
import { MissionHistoryView } from 'pages/MissionHistory/MissionHistoryView'
import { AutoScheduleSection } from 'pages/FrontPage/AutoScheduleSection/AutoScheduleSection'
import { RobotStatusSection } from 'pages/RobotCards/RobotStatusSection'

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
