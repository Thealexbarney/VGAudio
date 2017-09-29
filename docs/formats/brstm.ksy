#A Kaitai Struct format for Nintendo's BRSTM audio files
meta:
  id: brstm
  file-extension: brstm
  endian: be
seq:
  - id: magic
    contents: "RSTM"
  - id: bom
    type: s2
  - id: major_version
    type: s1      
  - id: minor_version
    type: s1
  - id: file_size
    type: s4
  - id: header_size
    type: s2
  - id: chunk_count
    type: s2
  - id: head_offset
    type: s4
  - id: head_size
    type: s4
  - id: adpc_offset
    type: s4
  - id: adpc_size
    type: s4
  - id: data_offset
    type: s4
  - id: data_size
    type: s4
types:
  pointer:
    seq:
      - id: marker
        type: s2
      - id: padding
        type: s2
      - id: offset
        type: s4
  head_chunk:
    seq:
      - id: magic
        contents: "HEAD"
      - id: size
        type: s4
      - id: stream_pointer
        type: pointer
      - id: track_pointer
        type: pointer
      - id: channel_pointer
        type: pointer
    instances:
      stream_info:
        pos: stream_pointer.offset + 8
        type: stream_info
      tracks_info:
        pos: track_pointer.offset + 8
        type: tracks_info
      channels_info:
        pos: channel_pointer.offset + 8
        type: channels_info
  stream_info:
    seq:
      - id: codec
        type: u1
        enum: audio_codec
      - id: is_looping
        type: b1
      - id: channel_count
        type: u1
      - id: padding
        size: 1
      - id: sample_rate
        type: s2
      - id: padding2
        size: 2
      - id: loop_start
        type: s4
      - id: sample_count
        type: s4
      - id: audio_data_offset
        type: s4
      - id: block_count
        type: s4
      - id: block_size_bytes
        type: s4
      - id: block_size_samples
        type: s4
      - id: final_block_size_bytes_without_padding
        type: s4
      - id: final_block_size_samples
        type: s4
      - id: final_block_size_bytes_with_padding
        type: s4
      - id: samples_per_seek_table_entry
        type: s4
      - id: bytes_per_seek_table_entry
        type: s4
    enums:
      audio_codec:
        0: pcm8
        1: pcm16
        2: adpcm
  tracks_info:
    seq:
      - id: track_count
        type: u1
      - id: type
        type: u1
        enum: track_type
      - id: padding
        size: 2
      - id: tracks
        type: track_pointer
        repeat: expr
        repeat-expr: track_count
  channels_info:
    seq:
      - id: channel_count
        type: u1
      - id : padding
        size: 3
      - id: channels
        type: channel_double_pointer
        repeat: expr
        repeat-expr: channel_count
  track_pointer:
    seq:
      - id: marker
        type: s2
      - id: padding
        type: s2
      - id: offset
        type: s4
    instances:
      track:
        pos: offset + 8
        type: track
  channel_double_pointer:
    seq:
      - id: marker
        type: s2
      - id: padding
        type: s2
      - id: offset
        type: s4
    instances:
      channel_pointer:
        pos: offset + 8
        type: channel_pointer
  channel_pointer:
    seq:
      - id: marker
        type: s2
      - id: padding
        type: s2
      - id: offset
        type: s4
    instances:
      channel:
        pos: offset + 8
        type: channel
  track:
    seq:
      - id: volume
        type: u1
        if: _parent.marker == 0x0101
      - id: panning
        size: 1
        if: _parent.marker == 0x0101
      - id: unknown1
        type: s2
        if: _parent.marker == 0x0101
      - id: unknown2
        type: s4
        if: _parent.marker == 0x0101
      - id: channel_count
        type: u1
      - id: left_channel_id
        type: u1
      - id: right_channel_id
        type: u1
      - id: padding
        size: 1
  channel:
    seq:
      - id: coefficients
        size: 32
      - id: gain
        type: s2
      - id: initial_predictor_scale
        type: s2
      - id: history1
        type: s2
      - id: history2
        type: s2
      - id: loop_predictor_scale
        type: s2
      - id: loop_history1
        type: s2
      - id: loop_history2
        type: s2
  adpc_chunk:
    seq:
      - id: magic
        contents: "ADPC"
      - id: size
        type: s4
      - id: seek_table
        type: seek_table_entry
        repeat: expr
        repeat-expr: (_root.head_chunk.stream_info.sample_count - 1) / _root.head_chunk.stream_info.samples_per_seek_table_entry + 1
  seek_table_entry:
    seq:
      - id: channels
        type: seek_table_entry_channel
        repeat: expr
        repeat-expr: _root.head_chunk.stream_info.channel_count
  seek_table_entry_channel:
    seq: 
      - id: history1
        type: s2
      - id: history2
        type: s2
  data_chunk:
    seq:
      - id: magic
        contents: "DATA"
      - id: size
        type: s4
      - id: padding_size
        type: s4
    instances:
      audio_data:
        pos: _root.head_chunk.stream_info.audio_data_offset - _root.data_offset
        type: audio_data
  audio_data:
    seq:
      - id: blocks
        type: audio_block
        repeat: expr
        repeat-expr: _root.head_chunk.stream_info.block_count - 1
      - id: final_block
        type: final_audio_block
  audio_block:
    seq:
      - id: channels
        size: _root.head_chunk.stream_info.block_size_bytes
        repeat: expr
        repeat-expr: _root.head_chunk.stream_info.channel_count
  final_audio_block:
    seq:
      - id: channels
        size: _root.head_chunk.stream_info.final_block_size_bytes_with_padding
        repeat: expr
        repeat-expr: _root.head_chunk.stream_info.channel_count
instances:
  head_chunk:
    pos: head_offset
    size: head_size
    type: head_chunk
  adpc_chunk:
    pos: adpc_offset
    size: adpc_size
    type: adpc_chunk
  data_chunk:
    pos: data_offset
    size: data_size
    type: data_chunk
enums:
  track_type:
    0: short
    1: long
