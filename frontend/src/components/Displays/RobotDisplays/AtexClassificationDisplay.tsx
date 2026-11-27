import { tokens } from '@equinor/eds-tokens'
import { Icon, Typography } from '@equinor/eds-core-react'
import styled from 'styled-components'
import { Icons } from 'utils/icons'
import { AtexClassification } from 'models/Robot'
import { useLanguageContext } from 'contexts/LanguageContext'
import { FieldLabel } from 'components/Styles/StyledComponents'

const Field = styled.div`
    display: flex;
    flex-direction: column;
    align-items: flex-start;
`
const ClassificationAlignment = styled.div`
    display: flex;
    gap: 7px;
    align-items: center;
`
const StyledTypography = styled(Typography)<{ $fontSize?: 24 | 16 | 18 | 32 | 40 | 48 }>`
    font-size: ${(props) => props.$fontSize}px;
`
interface AtexClassificationDisplayProps {
    classification: AtexClassification
    itemSize?: 24 | 16 | 18 | 32 | 40 | 48 | undefined
}

export const AtexClassificationDisplay = ({ classification, itemSize }: AtexClassificationDisplayProps) => {
    const { TranslateText } = useLanguageContext()
    if (classification === AtexClassification.Unknown) return null

    const label = TranslateText('ATEX classification')
    const color = tokens.colors.text.static_icons__default.hex

    return (
        <Field>
            <FieldLabel>{label}</FieldLabel>
            <ClassificationAlignment role="status">
                <Icon name={Icons.Info} color={color} size={itemSize} />
                <StyledTypography $fontSize={itemSize} color={color}>
                    {TranslateText(classification)}
                </StyledTypography>
            </ClassificationAlignment>
        </Field>
    )
}
