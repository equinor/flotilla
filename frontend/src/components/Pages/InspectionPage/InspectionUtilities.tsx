import { Card, Typography } from '@equinor/eds-core-react'
import styled from 'styled-components'
import { Inspection } from './InspectionSection'
import { getDeadlineInDays } from 'utils/StringFormatting'
import { tokens } from '@equinor/eds-tokens'
import { useLanguageContext } from 'components/Contexts/LanguageContext'

export const StyledDict = {
    Card: styled(Card)`
        display: flex;
        min-height: 150px;
        padding: 16px;
        flex-direction: column;
        justify-content: space-between;
        flex: 1 0 0;
        cursor: pointer;
        border-radius: 0px 4px 4px 0px;
    `,
    CardComponent: styled.div`
        display: flex;
        padding-right: 16px;
        justify-content: flex-end;
        gap: 10px;
        width: 100%;
    `,
    InspectionAreaCards: styled.div`
        display: grid;
        grid-template-columns: repeat(auto-fill, 450px);
        grid-auto-rows: 1fr;
        gap: 24px;
    `,
    InspectionAreaText: styled.div`
        display: grid;
        grid-template-rows: 25px 35px;
        align-self: stretch;
    `,
    TopInspectionAreaText: styled.div`
        display: flex;
        justify-content: space-between;
        margin-right: 5px;
    `,
    Rectangle: styled.div`
        display: flex-start;
        width: 24px;
        height: 100%;
        border-radius: 6px 0px 0px 6px;
    `,
    InspectionAreaCard: styled.div`
        display: flex;
        @media (max-width: 800px) {
            max-width: calc(100vw - 30px);
        }
        max-width: 450px;
        border-radius: 6px;
        box-shadow:
            0px 3px 4px 0px rgba(0, 0, 0, 0.12),
            0px 2px 4px 0px rgba(0, 0, 0, 0.14);
    `,
    Circle: styled.div`
        width: 13px;
        height: 13px;
        border-radius: 50px;
    `,
    MissionComponents: styled.div`
        display: flex;
        flex-direction: row;
        align-items: center;
        gap: 4px;
    `,
    InspectionAreaOverview: styled.div`
        display: flex;
        flex-direction: column;
        gap: 25px;
    `,
    MissionInspections: styled.div`
        display: flex;
        flex-direction: column;
        gap: 2px;
    `,
    Placeholder: styled.div`
        padding: 24px;
        border: 1px solid #dcdcdc;
        border-radius: 4px;
    `,
    Content: styled.div`
        display: flex;
        align-items: centre;
        gap: 5px;
    `,
    Buttons: styled.div`
        display: flex;
        flex-direction: row;
        gap: 8px;
        padding-bottom: 30px;
    `,
}

export enum InspectionAreaCardColors {
    Gray = 'gray',
    Green = 'green',
    Red = 'red',
    Orange = 'orange',
}

export const getDeadlineInspection = (deadline: Date): InspectionAreaCardColors => {
    const deadlineDays = getDeadlineInDays(deadline)
    switch (true) {
        case deadlineDays <= 1:
            return InspectionAreaCardColors.Red
        case deadlineDays > 1 && deadlineDays <= 7:
            return InspectionAreaCardColors.Red
        case deadlineDays > 7 && deadlineDays <= 14:
            return InspectionAreaCardColors.Orange
        case deadlineDays > 7 && deadlineDays <= 30:
            return InspectionAreaCardColors.Green
    }
    return InspectionAreaCardColors.Green
}

/**
 * Compares two inspections so that they can be sorted. An inspection with
 * an inspection frequency and/or a last run will be sorted before an inspection without.
 * If both inspections have an inspection frequency and/or a last run, the
 * inspection with the closest deadline will be sorted above the other.
 * @param inspection1 Arbitrary inspection to be compared
 * @param inspection2 Other arbitrary inspection to be compared
 * @returns positive number if the inspections should be sorted in the order
 *  inspection 1, inspection 2, and a negative number if the inspections should be
 *  sorted in the order inspection 2, inspection 1.
 */
export const compareInspections = (inspection1: Inspection, inspection2: Inspection) => {
    if (!inspection1.missionDefinition.inspectionFrequency && !inspection2.missionDefinition.inspectionFrequency) {
        if (!inspection1.missionDefinition.lastSuccessfulRun && !inspection2.missionDefinition.lastSuccessfulRun) {
            return inspection1.missionDefinition.name > inspection2.missionDefinition.name ? 1 : -1
        }
        if (!inspection1.missionDefinition.lastSuccessfulRun) return -1
        if (!inspection2.missionDefinition.lastSuccessfulRun) return 1
    } else if (!inspection1.missionDefinition.lastSuccessfulRun && !inspection2.missionDefinition.lastSuccessfulRun) {
        if (!inspection1.missionDefinition.inspectionFrequency) return 1
        if (!inspection2.missionDefinition.inspectionFrequency) return -1
    }
    if (!inspection1.missionDefinition.inspectionFrequency) return 1
    if (!inspection2.missionDefinition.inspectionFrequency) return -1
    if (!inspection1.missionDefinition.lastSuccessfulRun) return -1
    if (!inspection2.missionDefinition.lastSuccessfulRun) return 1
    else return inspection1.deadline!.getTime() - inspection2.deadline!.getTime()
}

interface ICardMissionInformationProps {
    inspectionAreaName: string
    inspections: Inspection[]
}

interface InspectionAreaMissionCount {
    [color: string]: {
        count: number
        message: string
    }
}

export const CardMissionInformation = ({ inspectionAreaName, inspections }: ICardMissionInformationProps) => {
    const { TranslateText } = useLanguageContext()

    var colorsCount: InspectionAreaMissionCount = {
        red: { count: 0, message: 'Must be inspected this week' },
        orange: { count: 0, message: 'Must be inspected within two weeks' },
        green: { count: 0, message: 'Up to date' },
        grey: { count: 0, message: '' },
    }

    inspections.forEach((inspection) => {
        if (!inspection.deadline) {
            if (!inspection.missionDefinition.lastSuccessfulRun && inspection.missionDefinition.inspectionFrequency) {
                colorsCount['red'].count++
            } else {
                colorsCount['green'].count++
            }
        } else {
            const dealineColor = getDeadlineInspection(inspection.deadline)
            colorsCount[dealineColor!].count++
        }
    })

    return (
        <StyledDict.MissionInspections>
            {Object.keys(colorsCount)
                .filter((color) => colorsCount[color].count > 0)
                .map((color) => (
                    <StyledDict.MissionComponents key={color}>
                        <StyledDict.Circle style={{ background: color }} />
                        <Typography color={tokens.colors.text.static_icons__secondary.rgba}>
                            {colorsCount[color].count > 1 &&
                                colorsCount[color].count +
                                    ' ' +
                                    TranslateText('Missions').toLowerCase() +
                                    ' ' +
                                    TranslateText(colorsCount[color].message).toLowerCase()}
                            {colorsCount[color].count === 1 &&
                                colorsCount[color].count +
                                    ' ' +
                                    TranslateText('Mission').toLowerCase() +
                                    ' ' +
                                    TranslateText(colorsCount[color].message).toLowerCase()}
                        </Typography>
                    </StyledDict.MissionComponents>
                ))}
        </StyledDict.MissionInspections>
    )
}
