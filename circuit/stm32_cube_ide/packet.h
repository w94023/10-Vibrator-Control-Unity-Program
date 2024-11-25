#ifndef __PACKET__
#define __PACKET__

#include "stm32f1xx_hal.h"

// 수신 패킷 사이즈
size_t packet_size = 14;

// Vibrator target intensity
uint8_t vibrator_target_intensity[10];

// PWM 채널 선언
extern TIM_HandleTypeDef htim1;
extern TIM_HandleTypeDef htim2;
extern TIM_HandleTypeDef htim3;
extern TIM_HandleTypeDef htim4;

// PWM1~10 핀에 대한 TIM, CHANNEL 설정
TIM_HandleTypeDef *timers[10] = {
    &htim1,
    &htim1,
    &htim1,
    &htim2,
    &htim2,
    &htim2,
    &htim2,
    &htim3,
    &htim3,
    &htim3
};
uint32_t vibrator_channels[10] = {
    TIM_CHANNEL_1, // PA8
    TIM_CHANNEL_2, // PA9
    TIM_CHANNEL_3, // PA10
    TIM_CHANNEL_1, // PA0
    TIM_CHANNEL_2, // PA1
    TIM_CHANNEL_3, // PA2
    TIM_CHANNEL_4, // PA3
    TIM_CHANNEL_1, // PA6
    TIM_CHANNEL_2, // PA7
    TIM_CHANNEL_3, // PB0
};

uint8_t get_checksum_byte(uint8_t *data, size_t start_index, size_t end_index)
{
    // start_index 부터 end_index 까지의 data byte를 기반으로 checksum byte 도출하는 메서드
    uint8_t checksum = 0;
    for (size_t i = start_index; i < end_index+1; i++) {
        checksum = checksum + data[i];
    }

    return ~(checksum);
}

bool verify_checksum(uint8_t *data, size_t start_index, size_t end_index, uint8_t checksum)
{
    // start_index 부터 end_index 까지의 data byte를 기반으로 계산된 checksum byte와 주어진 checksum byte 비교하는 메서드
    uint8_t checksum_from_data = get_checksum_byte(data, start_index, end_index);
    return checksum_from_data == checksum;
}

size_t find_header_index(uint8_t* data, size_t length)
{
    // 주어진 데이터 내에서 {0xFF, 0xFF, ~0xFF}로 시작되는 부분을 찾는 메서드
    size_t header_index = -1;
    
    // Search for the first and second occurrences of 0xFF, 0xFF
    for (size_t i = 0; i < length - 2; i++) {
        if (data[i] == 0xFF && data[i + 1] == 0xFF && data[i + 2] != 0xFF) {
            header_index = i; // Mark first occurrence
            break;
        }
    }

    return header_index;
}

void Analyze_Received_Packet(uint8_t *data, size_t length)
{
    // 수신 데이터 분석 메서드

    // 헤더 인식
    size_t header_index = find_header_index(data, length);
    if (header_index == -1) return;

    // 남은 패킷이 data length보다 클 경우 작업 종료
    if (length - packet_size < header_index) return;

    // 데이터 길이 인식
    int16_t data_length = (int16_t)data[header_index+2];
    if (data_length != 11) return; // 10 vibrator intensity + checksum

    // 체크섬 확인
    bool is_valid = verify_checksum(data, header_index+2, header_index+12, data[header_index+13]);
    if (!is_valid) return;

    // 진동자 출력 설정
    memcpy(vibrator_target_intensity, data + (header_index+3), 10);

    // PWM 채널에 진동자 출력 전달
    for (int i = 0; i < 10; i++) {
        Set_PWM_DutyCycle(timers[i], vibrator_channels[i], vibrator_target_intensity[i]);
    }

    // 수신 데이터 초기화
    memset(data, '\0', length);
}

void Set_PWM_DutyCycle(TIM_HandleTypeDef *htim, uint32_t channel, uint8_t duty_cycle)
{
    // PWM 설정하는 메서드
    if (duty_cycle <= 100) {
        uint32_t pulse = 0; // 기본값 설정
        if (duty_cycle > 0) {
            pulse = ((htim->Init.Period + 1) * duty_cycle / 100) - 1;
        }
        __HAL_TIM_SET_COMPARE(htim, channel, pulse);
    }
}

#endif // __PACKET__