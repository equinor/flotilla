import React from 'react'
import { Button, Typography } from '@equinor/eds-core-react'
import { MissionControlButtons } from 'components/Displays/MissionButtons/MissionControlButtons'
import { MissionStatusDisplay } from 'components/Displays/MissionDisplays/MissionStatusDisplay'
import { differenceInMinutes } from 'date-fns'
import { Mission, MissionStatus } from 'models/Mission'
import { tokens } from '@equinor/eds-tokens'
import styled from 'styled-components'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import { StatusReason } from '../StatusReason'
import { MissionRestartButton } from 'components/Displays/MissionButtons/MissionRestartButton'
import { TaskStatus } from 'models/Task'
import { convertUTCDateToLocalDate, formatDateTime } from 'utils/StringFormatting'
import { calculateRemaindingTimeInMinutes } from 'utils/CalculateRemaingingTime'
import { useNavigate } from 'react-router-dom'
import { phone_width } from 'utils/constants'
import { FieldLabel } from 'components/Styles/StyledComponents'
import { useContext } from 'react'
import { InstallationContext } from 'components/Contexts/InstallationContext'
import { RobotStatus } from 'models/Robot'
import { AnalysisType } from 'models/MissionDefinition'

const HeaderSection = styled.div`
    width: 100%;
    padding: 1.5rem 4rem;
    top: 56px;
    position: sticky;
    z-index: 1;
    background-color: ${tokens.colors.ui.background__light.hex};
    box-sizing: border-box;
    display: flex;
    align-items: flex-start;
    justify-content: space-between;
    gap: 2rem;
    @media (max-width: ${phone_width}) {
        flex-direction: column;
        padding: 1rem 1.5rem;
    }
`
const StaticHeaderSection = styled.div`
    width: 100%;
    padding: 1.5rem 4rem;
    background-color: ${tokens.colors.ui.background__light.hex};
    box-sizing: border-box;
    display: flex;
    align-items: flex-start;
    justify-content: space-between;
    gap: 2rem;
    @media (max-width: ${phone_width}) {
        flex-direction: column;
        padding: 1rem 1.5rem;
    }
`
const TitleArea = styled.div`
    display: flex;
    flex-direction: column;
    gap: 8px;
    flex: 1;
`
const ButtonGroup = styled.div`
    display: flex;
    flex-wrap: wrap;
    gap: 8px;
    align-items: center;
    flex-shrink: 0;
    @media (max-width: ${phone_width}) {
        width: 100%;
    }
`
const StyledTypography = styled(Typography)`
    font-family: Equinor;
    font-size: 32px;
    font-weight: 400;
    line-height: 40px;
    @media (max-width: ${phone_width}) {
        font-size: 24px;
    }
`
const MetricsSection = styled.div`
    width: 100%;
    padding: 1.25rem 4rem;
    background-color: ${tokens.colors.ui.background__light.hex};
    box-sizing: border-box;
    display: flex;
    flex-wrap: wrap;
    gap: 48px;
    @media (max-width: ${phone_width}) {
        padding: 1rem 1.5rem;
        display: grid;
        grid-template-columns: repeat(2, 1fr);
        gap: 1rem;
    }
`
const MetricItem = styled.div`
    display: flex;
    flex-direction: column;
    gap: 6px;
`
const Metric = ({ label, children }: { label: string; children: React.ReactNode }) => (
    <MetricItem>
        <FieldLabel>{label}</FieldLabel>
        {children}
    </MetricItem>
)

const getStartUsedAndRemainingTime = (
    mission: Mission,
    translatedMinutes: string
): {
    startTime: string
    startDate: string
    usedTime: string
    remainingTime: string
} => {
    let startTime: string
    let startDate: string
    let remainingTime: string
    let usedTimeInMinutes: number

    if (mission.endTime) {
        startTime = mission.startTime
            ? formatDateTime(mission.startTime, 'HH:mm')
            : formatDateTime(mission.endTime, 'HH:mm')

        startDate = mission.startTime
            ? formatDateTime(mission.startTime, 'dd/MM/yyy')
            : formatDateTime(mission.endTime, 'dd/MM/yyy')
        usedTimeInMinutes = mission.startTime
            ? differenceInMinutes(
                  convertUTCDateToLocalDate(mission.endTime),
                  convertUTCDateToLocalDate(mission.startTime)
              )
            : 0
        remainingTime = 'N/A'
    } else if (mission.startTime) {
        startTime = formatDateTime(mission.startTime, 'HH:mm')
        startDate = formatDateTime(mission.startTime, 'dd/MM/yyy')
        usedTimeInMinutes = differenceInMinutes(Date.now(), convertUTCDateToLocalDate(mission.startTime))
        if (mission.estimatedTaskDuration)
            remainingTime =
                calculateRemaindingTimeInMinutes(mission.tasks, mission.estimatedTaskDuration) + ' ' + translatedMinutes
        else remainingTime = 'N/A'
    } else {
        startTime = 'N/A'
        startDate = 'N/A'
        usedTimeInMinutes = 0
        if (mission.estimatedTaskDuration)
            remainingTime =
                calculateRemaindingTimeInMinutes(mission.tasks, mission.estimatedTaskDuration) + ' ' + translatedMinutes
        else remainingTime = 'N/A'
    }
    const usedTime: string = usedTimeInMinutes + ' ' + translatedMinutes
    return { startTime, startDate, usedTime, remainingTime }
}

export const MissionHeader = ({ mission }: { mission: Mission }) => {
    const { TranslateText } = useLanguageContext()
    const { installation } = useContext(InstallationContext)
    const navigate = useNavigate()
    const isMissionCompleted = mission.endTime ? true : false

    const translatedStartDate = TranslateText('Start date')
    const translatedStartTime = TranslateText('Start time')
    const translatedUsedTime = TranslateText('Time used')
    const translatedEstimatedTimeRemaining = TranslateText('Estimated time remaining')
    const translatedRobot = TranslateText('Robot')
    const translatedDescription = TranslateText('Description')
    const translatedTasks = TranslateText('Completed Tasks')
    const translatedStatus = TranslateText('Status')

    const translatedMinutes = TranslateText('minutes')
    const { startTime, startDate, usedTime, remainingTime } = getStartUsedAndRemainingTime(mission, translatedMinutes)
    const isMissionActive = mission.status === MissionStatus.Ongoing || mission.status === MissionStatus.Paused

    const missionHasFailedTasks = mission.tasks.some(
        (t) => t.status !== TaskStatus.PartiallySuccessful && t.status !== TaskStatus.Successful
    )

    const numberOfCompletedTasks = mission.tasks.filter((task) => task.isCompleted).length

    const canBePaused =
        mission.robot.status !== RobotStatus.GoingToRecharging &&
        mission.robot.status !== RobotStatus.RechargingWithMission

    const analysisType = mission.tasks
        .flatMap((task) => task.inspection?.analysisTypes ?? [])
        .find((type) => type === AnalysisType.Fencilla || type === AnalysisType.CLOE)

    const getDataOverviewUrl = (type: AnalysisType): string | undefined => {
        switch (type) {
            case AnalysisType.Fencilla:
                return `/${installation.installationCode}/fencilla-view`
            case AnalysisType.CLOE:
                return `/${installation.installationCode}/cloe-view`
            default:
                return undefined
        }
    }

    const dataOverviewUrl = analysisType ? getDataOverviewUrl(analysisType) : undefined

    return (
        <>
            <HeaderSection>
                <TitleArea>
                    <StyledTypography>{mission.name}</StyledTypography>
                    {mission.description && (
                        <Typography
                            variant="body_long"
                            group="paragraph"
                            color={tokens.colors.text.static_icons__secondary.hex}
                        >
                            {`${translatedDescription}: ${mission.description}`}
                        </Typography>
                    )}
                    <StatusReason statusReason={mission.statusReason} status={mission.status} />
                </TitleArea>
                <ButtonGroup>
                    {isMissionActive && (
                        <MissionControlButtons
                            canBePaused={canBePaused}
                            canBeSkipped={true}
                            missionName={mission.name}
                            robotId={mission.robot.id}
                            missionStatus={mission.status}
                        />
                    )}
                    {mission.endTime && (
                        <MissionRestartButton
                            mission={mission}
                            hasFailedTasks={missionHasFailedTasks}
                            smallButton={false}
                        />
                    )}
                    <Button
                        variant="outlined"
                        onClick={() =>
                            navigate(`/${installation.installationCode}/missiondefinition/${mission.missionId}`)
                        }
                    >
                        {TranslateText('View mission definition')}
                    </Button>
                    {dataOverviewUrl && (
                        <Button variant="outlined" onClick={() => navigate(dataOverviewUrl)}>
                            {TranslateText('Go to data overview')}
                        </Button>
                    )}
                </ButtonGroup>
            </HeaderSection>
            <MetricsSection>
                <Metric label={translatedStatus}>
                    <MissionStatusDisplay status={mission.status} />
                </Metric>
                <Metric label={translatedTasks}>
                    <Typography>
                        {numberOfCompletedTasks}/{mission.tasks.length}
                    </Typography>
                </Metric>
                <Metric label={translatedStartDate}>
                    <Typography>{startDate}</Typography>
                </Metric>
                <Metric label={translatedStartTime}>
                    <Typography>{startTime}</Typography>
                </Metric>
                <Metric label={translatedUsedTime}>
                    <Typography>{usedTime}</Typography>
                </Metric>
                {!isMissionCompleted && (
                    <Metric label={translatedEstimatedTimeRemaining}>
                        <Typography>{remainingTime}</Typography>
                    </Metric>
                )}
                <Metric label={translatedRobot}>
                    <Typography>{mission.robot.name}</Typography>
                </Metric>
            </MetricsSection>
        </>
    )
}

export const SimpleMissionHeader = ({ mission }: { mission: Mission }) => {
    const { TranslateText } = useLanguageContext()
    const isMissionCompleted = mission.endTime ? true : false
    const translatedStartDate = TranslateText('Start date')
    const translatedStartTime = TranslateText('Start time')
    const translatedUsedTime = TranslateText('Time used')
    const translatedEstimatedTimeRemaining = TranslateText('Estimated time remaining')
    const translatedRobot = TranslateText('Robot')
    const translatedDescription = TranslateText('Description')
    const translatedTasks = TranslateText('Completed Tasks')
    const translatedStatus = TranslateText('Status')

    const translatedMinutes = TranslateText('minutes')
    const { startTime, startDate, usedTime, remainingTime } = getStartUsedAndRemainingTime(mission, translatedMinutes)

    const numberOfCompletedTasks = mission.tasks.filter((task) => task.isCompleted).length

    return (
        <>
            <StaticHeaderSection>
                <TitleArea>
                    <StyledTypography>{mission.name}</StyledTypography>
                    {mission.description && (
                        <Typography
                            variant="body_long"
                            group="paragraph"
                            color={tokens.colors.text.static_icons__secondary.hex}
                        >
                            {`${translatedDescription}: ${mission.description}`}
                        </Typography>
                    )}
                    <StatusReason statusReason={mission.statusReason} status={mission.status} />
                </TitleArea>
            </StaticHeaderSection>
            <MetricsSection>
                <Metric label={translatedStatus}>
                    <MissionStatusDisplay status={mission.status} />
                </Metric>
                <Metric label={translatedTasks}>
                    <Typography>
                        {numberOfCompletedTasks}/{mission.tasks.length}
                    </Typography>
                </Metric>
                <Metric label={translatedStartDate}>
                    <Typography>{startDate}</Typography>
                </Metric>
                <Metric label={translatedStartTime}>
                    <Typography>{startTime}</Typography>
                </Metric>
                <Metric label={translatedUsedTime}>
                    <Typography>{usedTime}</Typography>
                </Metric>
                {!isMissionCompleted && (
                    <Metric label={translatedEstimatedTimeRemaining}>
                        <Typography>{remainingTime}</Typography>
                    </Metric>
                )}
                <Metric label={translatedRobot}>
                    <Typography>{mission.robot.name}</Typography>
                </Metric>
            </MetricsSection>
        </>
    )
}
