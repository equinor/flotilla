import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { MissionDefinitionHeader } from './MissionDefinitionHeader/MissionDefinitionHeader'
import { BackButton } from 'utils/BackButton'
import { BackendAPICaller } from 'api/ApiCaller'
import { Header } from 'components/Header/Header'
import { MissionDefinition } from 'models/MissionDefinition'
import { Button, Typography, Icon, Card } from '@equinor/eds-core-react'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import { MissionDefinitionUpdateForm } from 'models/MissionDefinitionUpdateForm'
import { config } from 'config'
import { Icons } from 'utils/icons'
import { tokens } from '@equinor/eds-tokens'
import { useMissionDefinitionsContext } from 'components/Contexts/MissionDefinitionsContext'
import { StyledPage } from 'components/Styles/StyledComponents'
import styled from 'styled-components'
import { AlertType, useAlertContext } from 'components/Contexts/AlertContext'
import { FailedRequestAlertContent, FailedRequestAlertListContent } from 'components/Alerts/FailedRequestAlert'
import { AlertCategory } from 'components/Alerts/AlertsBanner'
import { TaskTableAndMap } from '../MissionPage/TaskTableAndMap'
import { useQuery } from '@tanstack/react-query'
import { EditButton, FormContainer, FormItem, TitleComponent } from './MissionDefinitionStyledComponents'
import {
    MissionCommentEditDialog,
    MissionNameEditDialog,
    MissionSchedulingEditDialog,
} from 'components/Dialogs/MissionEditDialog'
import { formulateAutoScheduleFrequencyAsString } from 'utils/language'
import { useAssetContext } from 'components/Contexts/AssetContext'

const StyledCard = styled(Card)`
    display: flex;
    padding: 8px;
    min-height: 100px;
    height: auto
    border-radius: 6px;
    border: 1px solid ${tokens.colors.ui.background__medium.hex};
    overflow-y: hidden;
`
const StyledTableAndMap = styled.div`
    display: flex;
    width: fit-content;
`

const StyledTopComponents = styled.div`
    display: flex;
    justify-content: left;
    gap: 30px;
`
const StyledButton = styled(Button)`
    width: 160px;
`
const StyledMissionDefinitionPageBody = styled.div`
    display: flex;
    flex-direction: column;
    justify-items: stretch;
    gap: 30px;
`

const MetadataItem = ({
    title,
    content,
    onEdit,
    onDelete,
}: {
    title: string
    content: any
    onEdit?: () => void
    onDelete?: () => void
}) => {
    return (
        <FormItem>
            <StyledCard>
                <TitleComponent>
                    <Typography variant="body_long_bold" color={tokens.colors.text.static_icons__secondary.hex}>
                        {title}
                    </Typography>
                    {onEdit && (
                        <EditButton variant="ghost" onClick={onEdit}>
                            <Icon name={Icons.Edit} size={16} />
                        </EditButton>
                    )}
                    {onDelete && (
                        <EditButton variant="ghost" onClick={onDelete}>
                            <Icon name={Icons.Delete} size={16} />
                        </EditButton>
                    )}
                </TitleComponent>
                <Typography
                    variant="body_long"
                    group="paragraph"
                    color={tokens.colors.text.static_icons__secondary.hex}
                >
                    {content}
                </Typography>
            </StyledCard>
        </FormItem>
    )
}

const MissionDefinitionPageBody = ({ missionDefinition }: { missionDefinition: MissionDefinition }) => {
    const { TranslateText } = useLanguageContext()
    const { setAlert, setListAlert } = useAlertContext()

    const [isEditingName, setIsEditingName] = useState<boolean>(false)
    const [isEditingComment, setIsEditingComment] = useState<boolean>(false)
    const [isEditingSchedule, setIsEditingSchedule] = useState<boolean>(false)

    const lastMissionRun = useQuery({
        queryKey: ['fetchMissionRun', missionDefinition.lastSuccessfulRun?.id],
        queryFn: async () => {
            return await BackendAPICaller.getMissionRunById(missionDefinition.lastSuccessfulRun!.id)
        },
        retry: 2,
        retryDelay: 2000,
        enabled: missionDefinition.lastSuccessfulRun?.id !== undefined,
    }).data

    const onDeleteAutoSchedule = () => {
        const defaultMissionDefinitionForm: MissionDefinitionUpdateForm = {
            comment: missionDefinition.comment,
            schedulingTimesCETperWeek: [],
            name: missionDefinition.name,
            isDeprecated: false,
        }
        BackendAPICaller.updateMissionDefinition(missionDefinition.id, defaultMissionDefinitionForm).catch(() => {
            setAlert(
                AlertType.RequestFail,
                <FailedRequestAlertContent
                    translatedMessage={TranslateText('Failed to delete auto schedule frequency')}
                />,
                AlertCategory.ERROR
            )
            setListAlert(
                AlertType.RequestFail,
                <FailedRequestAlertListContent
                    translatedMessage={TranslateText('Failed to delete auto schedule frequency')}
                />,
                AlertCategory.ERROR
            )
        })
    }

    return (
        <StyledMissionDefinitionPageBody>
            <FormContainer>
                <MetadataItem
                    title={TranslateText('Name')}
                    content={missionDefinition.name}
                    onEdit={() => setIsEditingName(true)}
                />
                <MetadataItem
                    title={TranslateText('Inspection area')}
                    content={missionDefinition.inspectionArea.inspectionAreaName}
                />
                <MetadataItem
                    title={TranslateText('Automated scheduling')}
                    content={formulateAutoScheduleFrequencyAsString(
                        missionDefinition.autoScheduleFrequency,
                        TranslateText
                    )}
                    onEdit={() => setIsEditingSchedule(true)}
                    onDelete={onDeleteAutoSchedule}
                />
                <MetadataItem
                    title={TranslateText('Comment')}
                    content={missionDefinition.comment}
                    onEdit={() => setIsEditingComment(true)}
                />
            </FormContainer>

            {isEditingName && (
                <MissionNameEditDialog
                    mission={missionDefinition}
                    isOpen={isEditingName}
                    onClose={() => setIsEditingName(false)}
                />
            )}

            {isEditingComment && (
                <MissionCommentEditDialog
                    mission={missionDefinition}
                    isOpen={isEditingComment}
                    onClose={() => setIsEditingComment(false)}
                />
            )}

            {isEditingSchedule && (
                <MissionSchedulingEditDialog
                    mission={missionDefinition}
                    isOpen={isEditingSchedule}
                    onClose={() => setIsEditingSchedule(false)}
                />
            )}

            <StyledTableAndMap>
                {lastMissionRun && <TaskTableAndMap mission={lastMissionRun} missionDefinitionPage={true} />}
            </StyledTableAndMap>
        </StyledMissionDefinitionPageBody>
    )
}

export const MissionDefinitionPage = ({ missionId }: { missionId: string }) => {
    const { missionDefinitions } = useMissionDefinitionsContext()
    const { installationCode } = useAssetContext()
    const { TranslateText } = useLanguageContext()
    const navigate = useNavigate()

    const selectedMissionDefinition = missionDefinitions.find((m) => m.id === missionId)

    return (
        <>
            <Header page={'mission'} />
            {selectedMissionDefinition !== undefined && (
                <StyledPage>
                    <BackButton />
                    <StyledTopComponents>
                        <MissionDefinitionHeader missionDefinition={selectedMissionDefinition} />
                        <StyledButton
                            variant="outlined"
                            disabled={!selectedMissionDefinition.lastSuccessfulRun}
                            onClick={() =>
                                navigate(
                                    `${config.FRONTEND_BASE_ROUTE}/${installationCode}:mission?id=${selectedMissionDefinition.lastSuccessfulRun!.id}`
                                )
                            }
                        >
                            {TranslateText('View last run') +
                                (selectedMissionDefinition.lastSuccessfulRun
                                    ? ''
                                    : ': ' + TranslateText('Not yet performed'))}
                        </StyledButton>
                    </StyledTopComponents>
                    <MissionDefinitionPageBody missionDefinition={selectedMissionDefinition} />
                </StyledPage>
            )}
        </>
    )
}
